using UnityEditor;
using UnityEngine;

namespace GridMapSystem.Editor
{
    /// <summary>
    /// GridChunkDatabase를 이용해 청크를 그리드(10 단위 셀) 위에 4방향 자유 분기로 조합한다.
    /// 방식 1(ChunkMapGeneratorWindow, 픽셀 커서 기반)과는 완전히 독립된 창이다.
    /// </summary>
    public class GridChunkMapGeneratorWindow : EditorWindow
    {
        private GridChunkDatabase database;
        private int stepCount = 10;
        private int seed = 0;
        private bool useRandomSeed = true;
        [SerializeField] private SpawnPrefabSet spawnPrefabs = new SpawnPrefabSet();

        [MenuItem("Map/Generate Grid Map")]
        public static void Open()
        {
            GetWindow<GridChunkMapGeneratorWindow>("Grid Chunk Map Generator");
        }

        private void OnEnable()
        {
            if (database == null)
                database = AssetDatabase.LoadAssetAtPath<GridChunkDatabase>("Assets/Prefabs/@Data/Map/GridChunkDatabase.asset");
            if (spawnPrefabs.monsterPrefab == null)
                spawnPrefabs.monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Orc/OrcAI.prefab");
        }

        private void OnGUI()
        {
            database = (GridChunkDatabase)EditorGUILayout.ObjectField("Grid Chunk Database", database, typeof(GridChunkDatabase), false);
            stepCount = EditorGUILayout.IntField("Content Chunk Count", stepCount);
            useRandomSeed = EditorGUILayout.Toggle("Random Seed", useRandomSeed);
            if (!useRandomSeed)
                seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스폰 프리팹 (비워두면 그 종류는 스킵)", EditorStyles.boldLabel);
            spawnPrefabs.coinPrefab = (GameObject)EditorGUILayout.ObjectField("Coin", spawnPrefabs.coinPrefab, typeof(GameObject), false);
            spawnPrefabs.monsterPrefab = (GameObject)EditorGUILayout.ObjectField("Monster", spawnPrefabs.monsterPrefab, typeof(GameObject), false);
            spawnPrefabs.itemPrefab = (GameObject)EditorGUILayout.ObjectField("Item", spawnPrefabs.itemPrefab, typeof(GameObject), false);
            spawnPrefabs.arrowTrapPrefab = (GameObject)EditorGUILayout.ObjectField("Arrow Trap", spawnPrefabs.arrowTrapPrefab, typeof(GameObject), false);
            spawnPrefabs.spikeTrapPrefab = (GameObject)EditorGUILayout.ObjectField("Spike Trap", spawnPrefabs.spikeTrapPrefab, typeof(GameObject), false);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map (그리드)"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                ChunkGridGenerator.GenerateGrid(database, stepCount, actualSeed, spawnPrefabs);
            }
            if (GUILayout.Button("Clear Generated Grid Map"))
            {
                GameObject existingRoot = GameObject.Find(GridPlacementUtility.RootName);
                if (existingRoot != null) Object.DestroyImmediate(existingRoot);
            }
        }
    }
}
