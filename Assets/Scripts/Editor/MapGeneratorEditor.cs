using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapGeneratorEditor : EditorWindow
{
    private GameObject floorPrefab;
    private GameObject wideFloorPrefab;
    private int platformCount = 10;
    private float minJumpHorizontal = 3.0f;
    private float maxJumpHorizontal = 5.0f;
    private float maxJumpVertical = 2.0f;
    private Vector2 startPosition = Vector2.zero;
    private string mapName = "GeneratedMap";

    [MenuItem("Tools/Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<MapGeneratorEditor>("Map Generator");
    }

    private void OnEnable()
    {
        // Auto-load prefabs if they exist in expected paths
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

        platformCount = EditorGUILayout.IntField("Platform Count", platformCount);
        minJumpHorizontal = EditorGUILayout.FloatField("Min Jump Horizontal", minJumpHorizontal);
        maxJumpHorizontal = EditorGUILayout.FloatField("Max Jump Horizontal", maxJumpHorizontal);
        maxJumpVertical = EditorGUILayout.FloatField("Max Jump Vertical", maxJumpVertical);
        startPosition = EditorGUILayout.Vector2Field("Start Position", startPosition);

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

        Handles.color = Color.green;
        for (int i = 0; i < mapRoot.transform.childCount - 1; i++)
        {
            Vector3 start = mapRoot.transform.GetChild(i).position;
            Vector3 end = mapRoot.transform.GetChild(i + 1).position;
            Handles.DrawLine(start, end, 2.0f);
            
            // Draw a circle to show max jump range from each platform
            Handles.color = new Color(0, 1, 0, 0.1f);
            Handles.DrawSolidDisc(start, Vector3.forward, maxJumpHorizontal);
            Handles.color = Color.green;
        }
    }

    private void GenerateMap()
    {
        if (floorPrefab == null || wideFloorPrefab == null)
        {
            Debug.LogError("Prefabs not assigned!");
            return;
        }

        GameObject mapRoot = GameObject.Find(mapName);
        if (mapRoot == null)
        {
            mapRoot = new GameObject(mapName);
        }

        Vector2 currentPos = startPosition;
        List<GameObject> prefabs = new List<GameObject> { floorPrefab, wideFloorPrefab };

        for (int i = 0; i < platformCount; i++)
        {
            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject platform = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
            platform.transform.position = new Vector3(currentPos.x, currentPos.y, 0);
            platform.transform.SetParent(mapRoot.transform);
            platform.name = (i == 0) ? "StartPlatform" : (i == platformCount - 1) ? "ArrivalPlatform" : $"Platform_{i}";

            // Move to next position
            float nextX = currentPos.x + Random.Range(minJumpHorizontal, maxJumpHorizontal);
            float nextY = currentPos.y + Random.Range(-maxJumpVertical, maxJumpVertical);
            currentPos = new Vector2(nextX, nextY);
        }

        Undo.RegisterCreatedObjectUndo(mapRoot, "Generate Map");
        Selection.activeGameObject = mapRoot;
        Debug.Log($"Map '{mapName}' generated with {platformCount} platforms.");
    }

    private void ClearMap()
    {
        GameObject mapRoot = GameObject.Find(mapName);
        if (mapRoot != null)
        {
            Undo.DestroyObjectImmediate(mapRoot);
            Debug.Log($"Map '{mapName}' cleared.");
        }
    }
}
