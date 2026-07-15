using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapSystem.Editor
{
    /// <summary>
    /// ChunkDatabase를 이용해 청크를 순서대로 이어붙여 랜덤 맵을 생성한다.
    ///
    /// entrance1/entrance2는 "입구/출구"라는 고정된 역할이 아니라 그냥 "연결 가능한 두 지점"이다.
    /// 매 스텝마다 두 지점 중 어느 쪽이든 현재 커서에 연결해보고, 겹치지 않는 배치가 나오는
    /// 쪽을 채택한다. 방향 플래그나 진입 높이 필터는 없다 — 필요한 조건은 오직
    /// (1) 타일이 겹치지 않을 것, (2) 실제로 연결점이 이어질 것, 이 두 가지뿐이다.
    /// </summary>
    public class ChunkMapGeneratorWindow : EditorWindow
    {
        private const string RootName = "GeneratedMap";

        private enum Mode { AssembleMap, GenerateChunk }
        private Mode mode = Mode.AssembleMap;

        private ChunkDatabase database;
        private int stepCount = 10;
        private int seed = 0;
        private bool useRandomSeed = true;

        // Generate Chunk 모드 상태
        private ChunkType genChunkType = ChunkType.Combat;
        private ChunkDifficulty genDifficulty = ChunkDifficulty.Easy;
        private Vector2Int genEntrance1 = new Vector2Int(0, 3);
        private Vector2Int genEntrance2 = new Vector2Int(20, 3);
        private int genMaxJumpX = 6;
        private int genMaxRiseY = 3;
        private int genPlayerWidth = 1;
        private int genPlayerHeight = 2;
        private TileBase genGroundTile;
        private int genSeed = 0;
        private bool genUseRandomSeed = true;
        private string genChunkName = "NewChunk";
        private GameObject genPreviewRoot;
        private ChunkType lastGenChunkType;

        [MenuItem("Map/Generate Random Map")]
        public static void Open()
        {
            GetWindow<ChunkMapGeneratorWindow>("Chunk Map Generator");
        }

        private void OnEnable()
        {
            if (database == null)
                database = AssetDatabase.LoadAssetAtPath<ChunkDatabase>("Assets/Data/ChunkDatabase.asset");
            if (genGroundTile == null)
                genGroundTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/Map/ChunkMap/TileGround.asset");
            lastGenChunkType = genChunkType;
        }

        private void OnGUI()
        {
            mode = (Mode)GUILayout.Toolbar((int)mode, new[] { "Assemble Map", "Generate Chunk" });
            EditorGUILayout.Space();

            if (mode == Mode.AssembleMap)
                DrawAssembleMapGUI();
            else
                DrawGenerateChunkGUI();
        }

        private void DrawAssembleMapGUI()
        {
            database = (ChunkDatabase)EditorGUILayout.ObjectField("Chunk Database", database, typeof(ChunkDatabase), false);
            stepCount = EditorGUILayout.IntField("Step Count (Transition+Content 합)", stepCount);
            useRandomSeed = EditorGUILayout.Toggle("Random Seed", useRandomSeed);
            if (!useRandomSeed)
                seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                Generate(database, stepCount, actualSeed);
            }
            if (GUILayout.Button("Clear Generated Map"))
            {
                GameObject existingRoot = GameObject.Find(RootName);
                if (existingRoot != null) Object.DestroyImmediate(existingRoot);
            }
        }

        private void DrawGenerateChunkGUI()
        {
            GUILayout.Label("청크 자동 생성 (내부 지형 + 콘텐츠)", EditorStyles.boldLabel);

            genChunkType = (ChunkType)EditorGUILayout.EnumPopup("Chunk Type", genChunkType);
            if (genChunkType != lastGenChunkType)
            {
                int width = genChunkType == ChunkType.Transition ? 10 : 20;
                genEntrance2 = new Vector2Int(width, genEntrance2.y);
                lastGenChunkType = genChunkType;
            }
            if (genChunkType != ChunkType.Transition)
                genDifficulty = (ChunkDifficulty)EditorGUILayout.EnumPopup("Difficulty", genDifficulty);
            else
                genDifficulty = ChunkDifficulty.None;

            EditorGUILayout.Space();
            GUILayout.Label("Entrance (연결 가능 지점)", EditorStyles.boldLabel);
            genEntrance1 = EditorGUILayout.Vector2IntField("Entrance 1", genEntrance1);
            genEntrance2 = EditorGUILayout.Vector2IntField("Entrance 2", genEntrance2);

            EditorGUILayout.Space();
            GUILayout.Label("Player Limit", EditorStyles.boldLabel);
            genMaxJumpX = EditorGUILayout.IntField("Max Jump X (이동)", genMaxJumpX);
            genMaxRiseY = EditorGUILayout.IntField("Max Rise Y (상승)", genMaxRiseY);
            genPlayerWidth = EditorGUILayout.IntField("Player Width", genPlayerWidth);
            genPlayerHeight = EditorGUILayout.IntField("Player Height", genPlayerHeight);

            EditorGUILayout.Space();
            genGroundTile = (TileBase)EditorGUILayout.ObjectField("Ground Tile", genGroundTile, typeof(TileBase), false);
            genUseRandomSeed = EditorGUILayout.Toggle("Random Seed", genUseRandomSeed);
            if (!genUseRandomSeed)
                genSeed = EditorGUILayout.IntField("Seed", genSeed);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate"))
            {
                int actualSeed = genUseRandomSeed ? System.Environment.TickCount : genSeed;
                var settings = new ChunkAutoGenerator.Settings
                {
                    chunkType = genChunkType,
                    difficulty = genDifficulty,
                    width = genChunkType == ChunkType.Transition ? 10 : 20,
                    entrance1 = genEntrance1,
                    entrance2 = genEntrance2,
                    maxJumpX = genMaxJumpX,
                    maxRiseY = genMaxRiseY,
                    playerWidth = genPlayerWidth,
                    playerHeight = genPlayerHeight,
                    groundTile = genGroundTile,
                    seed = actualSeed,
                };
                genPreviewRoot = ChunkAutoGenerator.GeneratePreview(settings);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(genPreviewRoot == null))
            {
                genChunkName = EditorGUILayout.TextField("Chunk Name", genChunkName);
                if (GUILayout.Button("Save As Prefab"))
                {
                    ChunkAutoGenerator.SaveAsPrefab(genPreviewRoot, genChunkType, genDifficulty, genChunkName);
                    genPreviewRoot = null;
                }
            }
        }

        // 후보 하나를 커서에 연결하는 두 가지 방법(entrance1로 접합 / entrance2로 접합) 중
        // 겹치지 않는 쪽을 찾는다. 성공 시 true와 함께 원점·다음 커서를 반환한다.
        private static bool TryConnect(ChunkDatabaseEntry entry, Vector2 cursor, List<Rect> placedBoxes,
            System.Random rng, out Vector2 origin, out Vector2 nextCursor)
        {
            bool tryEntrance1First = rng.Next(2) == 0; // 두 방식 중 어느 쪽을 먼저 시도할지 무작위

            for (int attempt = 0; attempt < 2; attempt++)
            {
                bool useEntrance1 = (attempt == 0) ? tryEntrance1First : !tryEntrance1First;
                Vector2Int inSocket = useEntrance1 ? entry.entrance1 : entry.entrance2;
                Vector2Int outSocket = useEntrance1 ? entry.entrance2 : entry.entrance1;

                Vector2 candidateOrigin = cursor - new Vector2(inSocket.x, inSocket.y);
                Rect box = new Rect(candidateOrigin.x, candidateOrigin.y, entry.width, entry.height);

                if (!Overlaps(box, placedBoxes))
                {
                    origin = candidateOrigin;
                    nextCursor = candidateOrigin + new Vector2(outSocket.x, outSocket.y);
                    return true;
                }
            }

            origin = Vector2.zero;
            nextCursor = Vector2.zero;
            return false;
        }

        private static void Generate(ChunkDatabase db, int steps, int seed)
        {
            if (db == null) { Debug.LogError("[ChunkMapGenerator] ChunkDatabase가 없습니다."); return; }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            GameObject root = new GameObject(RootName);

            System.Random rng = new System.Random(seed);
            List<Rect> placedBoxes = new List<Rect>();

            Vector2 cursor = new Vector2(0f, 3f); // 시작 연결점
            int placedCount = 0;
            int failCount = 0;

            for (int step = 0; step < steps; step++)
            {
                bool wantTransition = (step % 2 == 0);
                ChunkType desiredType = wantTransition ? ChunkType.Transition : RandomContentType(rng);

                List<ChunkDatabaseEntry> candidates = new List<ChunkDatabaseEntry>(db.GetByTypeAndDifficulty(
                    desiredType, desiredType == ChunkType.Transition ? ChunkDifficulty.None : ChunkDifficulty.Easy));

                Shuffle(candidates, rng);

                bool placed = false;
                foreach (ChunkDatabaseEntry entry in candidates)
                {
                    if (!TryConnect(entry, cursor, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor))
                        continue;

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_" + step;

                    placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));
                    cursor = nextCursor;

                    placed = true;
                    placedCount++;
                    break;
                }

                if (!placed)
                {
                    failCount++;
                    Debug.LogWarning($"[ChunkMapGenerator] step {step}: 배치 가능한 후보 없음 (요구 타입={desiredType}, 커서={cursor}). 스킵.");
                }
            }

            Debug.Log($"[ChunkMapGenerator] 생성 완료: {placedCount}개 배치, {failCount}개 실패(스킵). seed={seed}");
        }

        private static ChunkType RandomContentType(System.Random rng)
        {
            ChunkType[] options = { ChunkType.Combat, ChunkType.Puzzle, ChunkType.Treasure };
            return options[rng.Next(options.Length)];
        }

        private static bool Overlaps(Rect box, List<Rect> others)
        {
            foreach (Rect o in others)
                if (box.Overlaps(o)) return true;
            return false;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
