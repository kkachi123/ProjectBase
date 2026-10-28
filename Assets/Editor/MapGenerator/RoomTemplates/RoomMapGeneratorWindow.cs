namespace ProjectRE.MapGeneration.Editor
{
    using ProjectRE;
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
                _goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/GridChunk/@BackGroundPrefab/GoalTrigger/EndLight-EndTrigger.prefab");
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
            var seed = new IntegerField("Seed") { value = _seed };
            seed.RegisterValueChangedCallback(e => _seed = e.newValue);
            rootVisualElement.Add(seed);
            rootVisualElement.Add(new Button(Generate) { text = "Generate Preview" });
            rootVisualElement.Add(new Button(ClearPreview) { text = "Clear This Preview" });
            _status = new Label("Preview uses the active scene and can be undone.");
            rootVisualElement.Add(_status);
        }

        private void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { _status.text = "Stop Play Mode before generating a preview."; return; }
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid())
            { _status.text = "Open a scene before generating a preview."; return; }

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
                _status.text = $"Preview activation failed: {exception.Message}";
                Undo.CollapseUndoOperations(group);
                return;
            }
            if (_previewRoot != null) Undo.DestroyObjectImmediate(_previewRoot);
            _previewRoot = info.gameObject;
            _ownedRoots.Add(_previewRoot);
            Undo.FlushUndoRecordObjects();
            Selection.activeGameObject = _previewRoot;
            Undo.CollapseUndoOperations(group);
            _status.text = $"Seed {plan.Seed}: {plan.Rooms.Count} rooms, {plan.Path.Count} on the path.";
        }

        private void ClearPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { _status.text = "Stop Play Mode before clearing a preview."; return; }
            if (_previewRoot == null) { _status.text = "No preview owned by this window."; return; }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clear Room Map Preview");
            Undo.DestroyObjectImmediate(_previewRoot);
            _previewRoot = null;
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            _status.text = "Preview cleared.";
        }
    }
}
