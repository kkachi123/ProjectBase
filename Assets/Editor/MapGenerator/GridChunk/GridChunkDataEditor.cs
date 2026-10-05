using UnityEditor;
using UnityEngine;

namespace GridMapSystem.Editor
{
    // GridChunkData의 spawnPoints를 씬 뷰에서 타일맵 찍듯 직접 편집할 수 있게 해주는 커스텀
    // 인스펙터. 좌표를 리스트에 손으로 입력하는 대신, 씬에서 클릭 한 번으로 찍고 뺄 수 있다.
    [CustomEditor(typeof(GridChunkData))]
    public class GridChunkDataEditor : UnityEditor.Editor
    {
        private bool editMode = false;
        private Tool savedTool = Tool.Move;
        private const float HandleSize = 0.3f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "편집 모드 켜고 씬 뷰에서:\n" +
                "- 빈 곳 클릭 → 스폰 포인트 추가\n" +
                "- 기존 점 Shift+클릭 → 삭제\n" +
                "- 기존 점 드래그 → 이동(정수 좌표로 스냅)",
                MessageType.Info);

            bool newEditMode = GUILayout.Toggle(editMode, "스폰 포인트 편집 모드", "Button");
            if (newEditMode != editMode)
            {
                editMode = newEditMode;
                if (editMode)
                    savedTool = Tools.current; // 끄고 나갈 때 원래 쓰던 툴(Move 등)로 복원하기 위해 저장
                else
                    Tools.current = savedTool;
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            if (!editMode) return;

            GridChunkData data = (GridChunkData)target;
            Transform t = data.transform;

            SerializedObject so = new SerializedObject(data);
            SerializedProperty spawnPointsProp = so.FindProperty("spawnPoints");

            Event e = Event.current;
            Handles.color = Color.yellow;

            // 빈 공간 클릭을 이 도구가 먼저 받도록 기본 컨트롤로 등록해둔다 — 이게 없으면
            // Unity가 빈 곳 클릭을 "오브젝트 선택 해제"로 먼저 처리해버려서 아무 반응이 없다.
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            // 편집 모드일 때 씬 뷰 전체에서 커서를 바꿔서 "클릭하면 뭔가 된다"는 걸 시각적으로 표시.
            // 기본 이동/회전 툴이 켜져 있으면 그쪽 기즈모가 클릭을 먼저 채가는 경우가 있어서
            // 편집 중에는 Tools.current를 꺼둔다.
            if (Camera.current != null)
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, Camera.current.pixelWidth, Camera.current.pixelHeight), MouseCursor.ArrowPlus);
            if (Tools.current != Tool.None)
                Tools.current = Tool.None;

            // 기존 포인트: 드래그로 이동, Shift+클릭으로 삭제
            for (int i = 0; i < spawnPointsProp.arraySize; i++)
            {
                SerializedProperty elem = spawnPointsProp.GetArrayElementAtIndex(i);
                Vector2Int p = elem.vector2IntValue;
                Vector3 worldPos = t.position + new Vector3(p.x, p.y, 0f);

                if (e.shift && e.type == EventType.MouseDown && e.button == 0
                    && HandleUtility.DistanceToCircle(worldPos, HandleSize) < 0.01f)
                {
                    spawnPointsProp.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    GUIUtility.hotControl = 0;
                    e.Use();
                    return;
                }

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.FreeMoveHandle(worldPos, HandleSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 local = moved - t.position;
                    Undo.RecordObject(data, "Move Spawn Point");
                    elem.vector2IntValue = new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
                    so.ApplyModifiedProperties();
                }
            }

            // 빈 곳 클릭 -> 새 포인트 추가 (Shift 안 눌렀을 때, 그리고 다른 핸들이 이 클릭을
            // 이미 가져가지 않았을 때만 — nearestControl로 확인)
            if (!e.shift && e.type == EventType.MouseDown && e.button == 0 && HandleUtility.nearestControl == controlId)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(Vector3.forward, t.position);
                if (plane.Raycast(ray, out float dist))
                {
                    Vector3 hit = ray.GetPoint(dist);
                    Vector3 local = hit - t.position;
                    Vector2Int newPoint = new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));

                    Undo.RecordObject(data, "Add Spawn Point");
                    so.Update();
                    spawnPointsProp.arraySize++;
                    spawnPointsProp.GetArrayElementAtIndex(spawnPointsProp.arraySize - 1).vector2IntValue = newPoint;
                    so.ApplyModifiedProperties();

                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }

            HandleUtility.Repaint();
        }
    }
}
