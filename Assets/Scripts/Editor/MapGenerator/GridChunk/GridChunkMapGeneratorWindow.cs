using UnityEditor;
using UnityEngine;
using GridMapSystem;

namespace GridMapSystem.Editor
{
    /// <summary>
    /// GridChunkDatabase를 이용해 청크를 그리드(10 단위 셀) 위에 4방향 자유 분기로 조합한다.
    /// 방식 1(ChunkMapGeneratorWindow, 픽셀 커서 기반)과는 완전히 독립된 창이다.
    /// </summary>
    public class GridChunkMapGeneratorWindow : EditorWindow
    {
        private GridMapGenerationSettings settings;
        private GridChunkDatabase database;
        private int stepCount = 10;
        private int maxThreeDirCount = 2;
        private int randomSeed = 0;
        private bool useRandomSeed = true;
        [SerializeField] private GridSpawnPrefabSet spawnPrefabs = new GridSpawnPrefabSet();

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
            settings = (GridMapGenerationSettings)EditorGUILayout.ObjectField("Generation Settings", settings, typeof(GridMapGenerationSettings), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load From Settings"))
                    LoadFromSettings();
                if (GUILayout.Button("Apply To Settings"))
                    ApplyToSettings();
            }

            EditorGUILayout.Space();
            database = (GridChunkDatabase)EditorGUILayout.ObjectField("Grid Chunk Database", database, typeof(GridChunkDatabase), false);
            stepCount = EditorGUILayout.IntField("Content Chunk Count", stepCount);
            maxThreeDirCount = EditorGUILayout.IntField("Max 3Direction Count", maxThreeDirCount);
            useRandomSeed = EditorGUILayout.Toggle("Random Seed", useRandomSeed);
            if (!useRandomSeed)
                randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스폰 프리팹 (비워두면 그 종류는 스킵)", EditorStyles.boldLabel);
            spawnPrefabs.coinPrefab = (GameObject)EditorGUILayout.ObjectField("Coin", spawnPrefabs.coinPrefab, typeof(GameObject), false);
            spawnPrefabs.monsterPrefab = (GameObject)EditorGUILayout.ObjectField("Monster", spawnPrefabs.monsterPrefab, typeof(GameObject), false);
            spawnPrefabs.itemPrefab = (GameObject)EditorGUILayout.ObjectField("Item", spawnPrefabs.itemPrefab, typeof(GameObject), false);
            spawnPrefabs.arrowTrapPrefab = (GameObject)EditorGUILayout.ObjectField("Arrow Trap", spawnPrefabs.arrowTrapPrefab, typeof(GameObject), false);
            spawnPrefabs.spikeTrapPrefab = (GameObject)EditorGUILayout.ObjectField("Spike Trap", spawnPrefabs.spikeTrapPrefab, typeof(GameObject), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Start/End 위치는 GeneratedMapInfo 컴포넌트(생성된 맵 루트)에 기록되고,\n실제 Player 생성/종료 처리는 런타임 GameManager가 그 값을 읽어서 처리합니다.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map (그리드)"))
            {   
                // Random seed : useRandomSeed가 true면 시간 기반으로 난수 생성기 초기화.
                ApplyToSettings();
                GridChunkRuntimeGenerator.GenerateGrid(database, stepCount, ResolveSeed(), spawnPrefabs, maxThreeDirCount, CreateEditorContext());
            }
            if (GUILayout.Button("Clear Generated Grid Map"))
            {
                GridChunkRuntimeGenerator.ClearGeneratedMap(CreateEditorContext());
            }
        }

        private void LoadFromSettings()
        {
            if (settings == null) return;

            database = settings.database;
            stepCount = settings.contentChunkCount;
            maxThreeDirCount = settings.maxThreeDirCount;
            useRandomSeed = settings.useRandomSeed;
            randomSeed = settings.randomSeed;
            spawnPrefabs = settings.spawnPrefabs ?? new GridSpawnPrefabSet();
        }

        private void ApplyToSettings()
        {
            if (settings == null) return;

            settings.database = database;
            settings.contentChunkCount = stepCount;
            settings.maxThreeDirCount = maxThreeDirCount;
            settings.useRandomSeed = useRandomSeed;
            settings.randomSeed = randomSeed;
            settings.spawnPrefabs = spawnPrefabs;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private int ResolveSeed()
        {
            return useRandomSeed ? System.Environment.TickCount : randomSeed;
        }

        private static GridChunkGenerationContext CreateEditorContext()
        {
            return new GridChunkGenerationContext
            {
                InstantiatePrefab = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent),
                DestroyObject = Object.DestroyImmediate,
            };
        }
    }
}
