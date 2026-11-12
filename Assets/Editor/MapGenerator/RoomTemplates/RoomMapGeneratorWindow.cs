namespace ProjectRE.MapGeneration.Editor
{
    using ProjectRE;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class RoomMapGeneratorWindow : EditorWindow
    {
        [SerializeField] private MapPlanningSettings _settings;
        [SerializeField] private GameObject _goalPrefab;
        [SerializeField] private int _seed;
        [SerializeField] private GameObject _previewRoot;
        private readonly List<GameObject> _ownedRoots = new();
        private Label _status;
        private Label _previewInfo;
        private IntegerField _seedField;
        private ScrollView _roomList;

        [MenuItem("Map/Room Map Generator")]
        public static void Open() => GetWindow<RoomMapGeneratorWindow>("Room Map");

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= RestorePreviewReference;
            Undo.undoRedoPerformed += RestorePreviewReference;
            if (_previewRoot != null && !_ownedRoots.Contains(_previewRoot)) _ownedRoots.Add(_previewRoot);
            if (_settings == null)
                _settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            if (_goalPrefab == null)
                _goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Goals/GoalTrigger/EndLight-EndTrigger.prefab");
        }

        private void OnDisable() => Undo.undoRedoPerformed -= RestorePreviewReference;

        private void RestorePreviewReference()
        {
            _previewRoot = null;
            for (int i = _ownedRoots.Count - 1; i >= 0; i--)
            {
                GameObject root = _ownedRoots[i];
                if (root != null && root.scene.IsValid() && root.scene.isLoaded && root.activeSelf)
                { _previewRoot = root; break; }
            }
            RefreshRoomList();
            if (_status != null) _status.text = "되돌리기 또는 다시 실행 결과를 반영했습니다.";
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var settings = new ObjectField("Planning Settings") { objectType = typeof(MapPlanningSettings), allowSceneObjects = false, value = _settings };
            settings.RegisterValueChangedCallback(e => _settings = (MapPlanningSettings)e.newValue);
            rootVisualElement.Add(settings);
            var goal = new ObjectField("Goal Prefab") { objectType = typeof(GameObject), allowSceneObjects = false, value = _goalPrefab };
            goal.RegisterValueChangedCallback(e => _goalPrefab = (GameObject)e.newValue);
            rootVisualElement.Add(goal);
            _seedField = new IntegerField("Seed") { value = _seed };
            _seedField.RegisterValueChangedCallback(e => _seed = e.newValue);
            rootVisualElement.Add(_seedField);
            rootVisualElement.Add(new Button(GenerateRandom) { text = "랜덤 맵 생성" });
            rootVisualElement.Add(new Button(Generate) { text = "입력한 Seed로 생성" });
            rootVisualElement.Add(new Button(FramePreview) { text = "맵 전체 보기" });
            rootVisualElement.Add(new Button(ClearPreview) { text = "미리보기 삭제" });
            _status = new Label("활성 씬에 미리보기를 생성합니다. Undo로 되돌릴 수 있습니다.");
            _status.AddToClassList("preview-status");
            rootVisualElement.Add(_status);
            _previewInfo = new Label();
            rootVisualElement.Add(_previewInfo);
            rootVisualElement.Add(new Label("방 목록 (선택하면 Scene 뷰에서 표시)"));
            _roomList = new ScrollView();
            _roomList.AddToClassList("room-list");
            rootVisualElement.Add(_roomList);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MapGenerator/RoomTemplates/RoomMapGeneratorWindow.uss");
            if (style != null && !rootVisualElement.styleSheets.Contains(style)) rootVisualElement.styleSheets.Add(style);
            RefreshRoomList();
        }

        private void GenerateRandom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { _status.text = "Play 모드를 종료한 뒤 미리보기를 생성하세요."; return; }
            _seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
            _seedField.SetValueWithoutNotify(_seed);
            Generate();
        }

        private void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { _status.text = "Play 모드를 종료한 뒤 미리보기를 생성하세요."; return; }
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid())
            { _status.text = "씬을 연 뒤 미리보기를 생성하세요."; return; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Room Map Preview");
            var context = new MapBuildContext
            {
                CreateRoot = name => { var root = new GameObject(name); Undo.RegisterCreatedObjectUndo(root, "Create map"); return root; },
                InstantiatePrefab = (prefab, parent) =>
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    if (instance != null) Undo.RegisterCreatedObjectUndo(instance, "Create map object");
                    return instance;
                },
                AddComponent = (target, type) => Undo.AddComponent(target, type),
                RecordComponent = target => Undo.RecordObject(target, "Set map positions"),
                SetParent = (child, parent) => Undo.SetTransformParent(child, parent, "Parent map object"),
                SetActive = (target, active) =>
                {
                    Undo.RecordObject(target, "Set hazard state");
                    target.SetActive(active);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                },
                DestroyObject = target => Undo.DestroyObjectImmediate(target),
            };
            if (!RoomMapFactory.TryCreate(_settings, _seed, _goalPrefab, context, out var info, out var plan, out string error))
            { _status.text = error; Undo.CollapseUndoOperations(group); return; }

            try
            {
                Undo.RecordObject(info.gameObject, "Activate map");
                info.gameObject.SetActive(true);
                Undo.FlushUndoRecordObjects();
            }
            catch (System.Exception exception)
            {
                context.DestroyObject(info.gameObject);
                _status.text = $"미리보기 활성화 실패: {exception.Message}";
                Undo.CollapseUndoOperations(group);
                return;
            }
            if (_previewRoot != null) Undo.DestroyObjectImmediate(_previewRoot);
            _previewRoot = info.gameObject;
            _ownedRoots.Add(_previewRoot);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            _status.text = $"Seed {plan.Seed}: 전체 {plan.Rooms.Count}개 방, 주 경로 {plan.Path.Count}개";
            RefreshRoomList();
            FramePreview();
        }

        private void ClearPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { _status.text = "Play 모드를 종료한 뒤 미리보기를 삭제하세요."; return; }
            if (_previewRoot == null) { _status.text = "이 창에서 생성한 미리보기가 없습니다."; return; }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clear Room Map Preview");
            Undo.DestroyObjectImmediate(_previewRoot);
            _previewRoot = null;
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            _status.text = "미리보기를 삭제했습니다.";
            RefreshRoomList();
        }

        private void RefreshRoomList()
        {
            if (_roomList == null) return;
            _roomList.Clear();
            _previewInfo.text = _previewRoot == null ? "현재 미리보기 없음" : $"현재 미리보기: {_previewRoot.name}";
            if (_previewRoot == null) return;
            foreach (var room in _previewRoot.GetComponentsInChildren<RoomTemplate>(true))
            {
                Vector3 position = room.transform.localPosition;
                int x = Mathf.RoundToInt(position.x / RoomTemplate.RoomSize);
                int y = Mathf.RoundToInt(-position.y / RoomTemplate.RoomSize);
                var button = new Button(() => FrameRoom(room))
                { text = $"({x}, {y})  {room.TemplateId}  [{FormatOpenings(room.Openings)}]" };
                button.AddToClassList("room-button");
                _roomList.Add(button);
            }
        }

        private static string FormatOpenings(RoomOpening openings)
        {
            var names = new List<string>();
            if ((openings & RoomOpening.North) != 0) names.Add("북");
            if ((openings & RoomOpening.East) != 0) names.Add("동");
            if ((openings & RoomOpening.South) != 0) names.Add("남");
            if ((openings & RoomOpening.West) != 0) names.Add("서");
            return names.Count == 0 ? "없음" : string.Join("·", names);
        }

        private void FramePreview()
        {
            if (_previewRoot == null) { _status.text = "생성된 미리보기가 없습니다."; return; }
            var rooms = _previewRoot.GetComponentsInChildren<RoomTemplate>(true);
            if (rooms.Length == 0) return;
            Bounds bounds = RoomBounds(rooms[0]);
            foreach (var room in rooms) bounds.Encapsulate(RoomBounds(room));
            Selection.activeGameObject = _previewRoot;
            SceneView.lastActiveSceneView?.Frame(bounds, true);
        }

        private static void FrameRoom(RoomTemplate room)
        {
            if (room == null) return;
            Selection.activeGameObject = room.gameObject;
            SceneView.lastActiveSceneView?.Frame(RoomBounds(room), true);
        }

        private static Bounds RoomBounds(RoomTemplate room)
        {
            float size = RoomTemplate.RoomSize;
            return new Bounds(room.transform.TransformPoint(new Vector3(size / 2, size / 2, 0)),
                new Vector3(size, size, 1));
        }
    }
}
