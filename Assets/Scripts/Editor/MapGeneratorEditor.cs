using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapGeneratorEditor : EditorWindow
{
    private GameObject floorPrefab;
    private GameObject wideFloorPrefab;
    private int platformCount = 15;
    private Vector2 startPosition = Vector2.zero;
    private Vector2 arrivalPosition = new Vector2(50, 0);
    
    [Header("Physics Settings")]
    private float jumpVelocityX = 8.0f;
    private float jumpVelocityY = 12.0f;
    private float gravity = 20.0f;
    
    [Header("Generation Settings")]
    private float pathWidth = 10.0f; // 좌우 분기 너비
    private string mapName = "GeneratedMap";

    [MenuItem("Tools/Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<MapGeneratorEditor>("Map Generator");
    }

    private void OnEnable()
    {
        if (floorPrefab == null)
            floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BackGround/Floor/Floor.prefab");
        if (wideFloorPrefab == null)
            wideFloorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BackGround/Floor/WideFloor.prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Generation Settings", EditorStyles.boldLabel);

        mapName = EditorGUILayout.TextField("Map Name", mapName);
        floorPrefab = (GameObject)EditorGUILayout.ObjectField("Floor Prefab", floorPrefab, typeof(GameObject), false);
        wideFloorPrefab = (GameObject)EditorGUILayout.ObjectField("Wide Floor Prefab", wideFloorPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        platformCount = EditorGUILayout.IntField("Platform Count", platformCount);
        pathWidth = EditorGUILayout.FloatField("Branch Width", pathWidth);
        startPosition = EditorGUILayout.Vector2Field("Start Position", startPosition);
        arrivalPosition = EditorGUILayout.Vector2Field("Arrival Position", arrivalPosition);

        EditorGUILayout.Space();
        GUILayout.Label("Physics & Jump Validation", EditorStyles.boldLabel);
        jumpVelocityX = EditorGUILayout.FloatField("Jump Velocity X", jumpVelocityX);
        jumpVelocityY = EditorGUILayout.FloatField("Jump Velocity Y", jumpVelocityY);
        gravity = EditorGUILayout.FloatField("Gravity", gravity);

        if (GUILayout.Button("Generate Map"))
        {
            GenerateMap();
        }

        if (GUILayout.Button("Clear Map"))
        {
            ClearMap();
        }
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        GameObject mapRoot = GameObject.Find(mapName);
        if (mapRoot == null) return;

        for (int i = 0; i < mapRoot.transform.childCount; i++)
        {
            Transform current = mapRoot.transform.GetChild(i);
            float currentWidth = GetPlatformWidth(current.gameObject);

            // 해당 플랫폼에서 갈 수 있는 최대 범위 시각화 (좌우 끝 기준)
            float maxReach = GetMaxJumpWidth(jumpVelocityX, jumpVelocityY, gravity);
            
            Handles.color = new Color(0, 1, 0, 0.05f);
            // 왼쪽 끝과 오른쪽 끝에서 각각 원을 그려 점프 가능 영역 표시
            Vector3 leftEdge = current.position + Vector3.left * (currentWidth / 2);
            Vector3 rightEdge = current.position + Vector3.right * (currentWidth / 2);
            Handles.DrawWireDisc(leftEdge, Vector3.forward, maxReach);
            Handles.DrawWireDisc(rightEdge, Vector3.forward, maxReach);

            // 다음 플랫폼들과의 연결선
            for (int j = i + 1; j < Mathf.Min(i + 3, mapRoot.transform.childCount); j++)
            {
                Transform next = mapRoot.transform.GetChild(j);
                float nextWidth = GetPlatformWidth(next.gameObject);

                float gapX = Mathf.Max(0, Mathf.Abs(next.position.x - current.position.x) - (currentWidth / 2 + nextWidth / 2));
                float diffY = next.position.y - current.position.y;

                bool reachable = IsJumpReachable(gapX, diffY, jumpVelocityX, jumpVelocityY, gravity);
                Handles.color = reachable ? Color.green : Color.red;
                Handles.DrawLine(current.position, next.position, 1.5f);
            }
        }
    }

    private float GetPlatformWidth(GameObject obj)
    {
        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col != null) return col.size.x * obj.transform.localScale.x;
        return 1.0f;
    }

    private float GetMaxJumpWidth(float vX, float vY, float gravity)
    {
        float t = (vY + Mathf.Sqrt(vY * vY)) / gravity; // 대략적인 체공시간
        return vX * t;
    }

    private bool IsJumpReachable(float gapX, float diffY, float vX, float vY, float gravity)
    {
        float maxHeight = (vY * vY) / (2f * gravity);
        if (diffY > maxHeight * 0.95f) return false;

        float a = 0.5f * gravity;
        float b = -vY;
        float c = diffY;
        float determinant = b * b - 4f * a * c;

        if (determinant < 0) return false;

        float timeToLand = (-b + Mathf.Sqrt(determinant)) / (2f * a);
        float maxJumpWidth = vX * timeToLand;

        return gapX <= maxJumpWidth;
    }

    private void GenerateMap()
    {
        if (floorPrefab == null || wideFloorPrefab == null)
        {
            Debug.LogError("Prefabs not assigned!");
            return;
        }

        ClearMap();
        GameObject mapRoot = new GameObject(mapName);

        Vector2 currentPos = startPosition;
        List<GameObject> prefabs = new List<GameObject> { floorPrefab, wideFloorPrefab };
        
        float maxJumpDist = GetMaxJumpWidth(jumpVelocityX, jumpVelocityY, gravity);

        // 1. 시작 플랫폼
        CreatePlatform(floorPrefab, startPosition, "StartPlatform", mapRoot.transform);

        // 2. 중간 플랫폼 생성 (분기 포함)
        int steps = platformCount - 2;
        Vector2 mainDir = (arrivalPosition - startPosition).normalized;
        float totalDist = Vector2.Distance(startPosition, arrivalPosition);
        float distPerStep = totalDist / (steps + 1);

        for (int i = 1; i <= steps; i++)
        {
            float progress = (float)i / (steps + 1);
            Vector2 basePos = Vector2.Lerp(startPosition, arrivalPosition, progress);
            
            // 좌우 분기 결정 (예: 30% 확률로 좌우 두 개 생성)
            bool isBranch = Random.value < 0.4f && i < steps;
            
            if (isBranch)
            {
                // 왼쪽 경로
                Vector2 leftPos = basePos + new Vector2(-pathWidth * Random.Range(0.5f, 1f), Random.Range(-2f, 2f));
                CreateValidPlatform(prefabs, leftPos, currentPos, $"Platform_{i}_L", mapRoot.transform);
                // 오른쪽 경로
                Vector2 rightPos = basePos + new Vector2(pathWidth * Random.Range(0.5f, 1f), Random.Range(-2f, 2f));
                CreateValidPlatform(prefabs, rightPos, currentPos, $"Platform_{i}_R", mapRoot.transform);
                
                // 다음 기준점은 두 분기 중 하나로 랜덤 설정 (단순화)
                currentPos = Random.value > 0.5f ? leftPos : rightPos;
                i++; // 한 스텝 더 소비
            }
            else
            {
                // 일반 경로 (약간의 랜덤 오프셋, 점프 사거리 밖으로 유도)
                Vector2 offset = new Vector2(Random.Range(-2f, 2f), Random.Range(-3f, 3f));
                Vector2 targetPos = basePos + offset;
                
                currentPos = CreateValidPlatform(prefabs, targetPos, currentPos, $"Platform_{i}", mapRoot.transform);
            }
        }

        // 3. 도착 플랫폼
        CreatePlatform(wideFloorPrefab, arrivalPosition, "ArrivalPlatform", mapRoot.transform);

        Undo.RegisterCreatedObjectUndo(mapRoot, "Generate Map");
        Selection.activeGameObject = mapRoot;
    }

    private Vector2 CreateValidPlatform(List<GameObject> prefabs, Vector2 targetPos, Vector2 lastPos, string name, Transform parent)
    {
        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        float prefabWidth = GetPlatformWidth(prefab);
        
        // 이전 플랫폼과의 거리 체크 및 조정 (도전적인 점프 유도)
        float maxJump = GetMaxJumpWidth(jumpVelocityX, jumpVelocityY, gravity);
        float minRequiredGap = maxJump * 0.6f; // 최소 점프 거리 (도전성 부여)
        
        Vector2 dir = (targetPos - lastPos).normalized;
        float dist = Vector2.Distance(targetPos, lastPos);
        
        // 너무 가까우면 뒤로 미룸
        if (dist < minRequiredGap) dist = minRequiredGap + Random.Range(0, 2f);
        // 너무 멀면 당김
        if (dist > maxJump + prefabWidth) dist = maxJump + (prefabWidth * 0.5f);

        Vector2 finalPos = lastPos + dir * dist;
        CreatePlatform(prefab, finalPos, name, parent);
        return finalPos;
    }

    private void CreatePlatform(GameObject prefab, Vector2 pos, string name, Transform parent)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.transform.SetParent(parent);
        obj.name = name;
    }

    private void ClearMap()
    {
        GameObject mapRoot = GameObject.Find(mapName);
        if (mapRoot != null)
        {
            Undo.DestroyObjectImmediate(mapRoot);
        }
    }
}
