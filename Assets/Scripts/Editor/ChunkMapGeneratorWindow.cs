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
        private ChunkEntrancePoint genEntrance1Point = ChunkEntrancePoint.West1;
        private ChunkEntrancePoint genEntrance2Point = ChunkEntrancePoint.East1;
        private int genMaxJumpX = 6;
        private int genMaxRiseY = 3;
        private int genPlayerWidth = 1;
        private int genPlayerHeight = 2;
        private TileBase genGroundTile;
        private int genSeed = 0;
        private bool genUseRandomSeed = true;
        private string genChunkName = "NewChunk";
        private GameObject genPreviewRoot;

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
            if (GUILayout.Button("Generate Map (선형)"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                Generate(database, stepCount, actualSeed);
            }
            if (GUILayout.Button("Generate Map (가지치기 — 다방향 청크에서 분기)"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                GenerateBranching(database, stepCount, actualSeed);
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
            if (genChunkType != ChunkType.Transition)
                genDifficulty = (ChunkDifficulty)EditorGUILayout.EnumPopup("Difficulty", genDifficulty);
            else
                genDifficulty = ChunkDifficulty.None;

            int genWidth = genChunkType == ChunkType.Transition ? 10 : 20;

            EditorGUILayout.Space();
            GUILayout.Label("Entrance (연결 가능 지점 — 청크 4변 x 2지점)", EditorStyles.boldLabel);
            genEntrance1Point = (ChunkEntrancePoint)EditorGUILayout.EnumPopup("Entrance 1", genEntrance1Point);
            genEntrance2Point = (ChunkEntrancePoint)EditorGUILayout.EnumPopup("Entrance 2", genEntrance2Point);
            if (genEntrance1Point == genEntrance2Point)
                EditorGUILayout.HelpBox("두 entrance가 같은 지점입니다 — 문이 하나뿐인 '구석진 방'으로 생성됩니다.", MessageType.Info);
            Vector2Int genEntrance1 = ChunkAutoGenerator.ResolveEntrancePoint(genEntrance1Point, genWidth, 10);
            Vector2Int genEntrance2 = ChunkAutoGenerator.ResolveEntrancePoint(genEntrance2Point, genWidth, 10);
            EditorGUILayout.LabelField("실제 좌표", $"{genEntrance1}  ↔  {genEntrance2}");

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
                    width = genWidth,
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

        // 후보 하나를 커서에 연결한다. entrances 목록(N개) 중 하나를 입구로 삼아 시도하고,
        // 배치가 겹치지 않으면 나머지 진입점 중 하나를 무작위로 골라 출구(다음 커서)로 사용한다.
        // 진입점이 2개뿐인 기존 청크는 이전과 완전히 동일하게 동작한다(둘 중 하나가 입구면
        // 나머지 하나가 자동으로 출구가 됨).
        private static bool TryConnect(ChunkDatabaseEntry entry, Vector2 cursor, List<Rect> placedBoxes,
            System.Random rng, out Vector2 origin, out Vector2 nextCursor)
        {
            List<Vector2Int> entrances = entry.GetAllEntrances();

            List<int> order = new List<int>();
            for (int i = 0; i < entrances.Count; i++) order.Add(i);
            Shuffle(order, rng);

            foreach (int inIdx in order)
            {
                Vector2Int inSocket = entrances[inIdx];
                Vector2 candidateOrigin = cursor - new Vector2(inSocket.x, inSocket.y);
                Rect box = new Rect(candidateOrigin.x, candidateOrigin.y, entry.width, entry.height);

                if (Overlaps(box, placedBoxes)) continue;

                List<int> otherIdx = new List<int>();
                for (int i = 0; i < entrances.Count; i++) if (i != inIdx) otherIdx.Add(i);
                if (otherIdx.Count == 0) continue; // 진입점이 1개뿐이면(막다른길) 연결 대상에서 제외

                int outIdx = otherIdx[rng.Next(otherIdx.Count)];
                Vector2Int outSocket = entrances[outIdx];

                origin = candidateOrigin;
                nextCursor = candidateOrigin + new Vector2(outSocket.x, outSocket.y);
                return true;
            }

            origin = Vector2.zero;
            nextCursor = Vector2.zero;
            return false;
        }

        // 한 스텝의 배치 시도 기록 -- 백트래킹 시 이 프레임 단위로 되돌린다.
        private class PlacementFrame
        {
            public ChunkType desiredType;
            public Vector2 cursorBefore;
            public HashSet<ChunkDatabaseEntry> excluded = new HashSet<ChunkDatabaseEntry>();
            public ChunkDatabaseEntry placedEntry;
            public GameObject instance;
            public Vector2 cursorAfter;
        }

        // 막히면 그냥 스킵하는 대신, 이전 스텝으로 돌아가 다른 청크를 다시 골라본다(백트래킹).
        // 이전 스텝도 더 이상 대안이 없으면 그보다 더 앞으로 계속 되돌아간다.
        private static void Generate(ChunkDatabase db, int steps, int seed)
        {
            if (db == null) { Debug.LogError("[ChunkMapGenerator] ChunkDatabase가 없습니다."); return; }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            GameObject root = new GameObject(RootName);

            System.Random rng = new System.Random(seed);
            List<Rect> placedBoxes = new List<Rect>();
            List<PlacementFrame> stack = new List<PlacementFrame>();

            const int MAX_BACKTRACKS = 3000;
            int totalBacktracks = 0;
            int step = 0;

            while (step < steps)
            {
                if (step == stack.Count)
                {
                    bool wantTransition = rng.Next(2) == 0; // 엄격한 교대 대신 무작위 선택(Content-Content, Transition-Transition 연속 허용)
                    ChunkType desiredType = wantTransition ? ChunkType.Transition : RandomContentType(rng);
                    Vector2 cursorBefore = (step == 0) ? new Vector2(0f, 3f) : stack[step - 1].cursorAfter;
                    stack.Add(new PlacementFrame { desiredType = desiredType, cursorBefore = cursorBefore });
                }

                PlacementFrame frame = stack[step];
                List<ChunkDatabaseEntry> candidates = new List<ChunkDatabaseEntry>(db.GetByType(frame.desiredType));
                candidates.RemoveAll(e => frame.excluded.Contains(e));
                Shuffle(candidates, rng);

                bool placed = false;
                foreach (ChunkDatabaseEntry entry in candidates)
                {
                    if (!TryConnect(entry, frame.cursorBefore, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor))
                        continue;

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_" + step;

                    placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));

                    frame.placedEntry = entry;
                    frame.instance = inst;
                    frame.cursorAfter = nextCursor;

                    step++;
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    if (step == 0)
                    {
                        Debug.LogError("[ChunkMapGenerator] 첫 스텝부터 배치 가능한 청크가 없습니다 — 생성 중단.");
                        break;
                    }

                    totalBacktracks++;
                    if (totalBacktracks > MAX_BACKTRACKS)
                    {
                        Debug.LogWarning($"[ChunkMapGenerator] 백트래킹 한도({MAX_BACKTRACKS}회) 초과 — {step}개까지만 배치하고 중단합니다.");
                        break;
                    }

                    // 이전 스텝으로 되돌아가서 그 자리는 다른 청크로 다시 시도한다.
                    step--;
                    PlacementFrame prevFrame = stack[step];
                    if (prevFrame.instance != null) Object.DestroyImmediate(prevFrame.instance);
                    placedBoxes.RemoveAt(placedBoxes.Count - 1);
                    prevFrame.excluded.Add(prevFrame.placedEntry);
                    prevFrame.placedEntry = null;
                    prevFrame.instance = null;
                    if (stack.Count > step + 1) stack.RemoveRange(step + 1, stack.Count - step - 1);
                }
            }

            Debug.Log($"[ChunkMapGenerator] 생성 완료: {step}개 배치 (요청 {steps}개), 백트래킹 {totalBacktracks}회. seed={seed}");
        }


        // ================= 가지치기(branching) 맵 생성 =================
        // 메인 경로는 기존 선형 생성기(백트래킹 포함)와 동일하게 진행하되, 다방향(3개 이상
        // 진입점) 청크를 만나면 실제 사용하지 않은 나머지 진입점들을 '사이드 브랜치 대기열'에
        // 넣어둔다. 메인 경로가 끝나면 대기열을 순서대로 처리해서, 각 사이드 브랜치를 무작위
        // 스텝(2~5) 진행한 뒤 막다른길(단일 진입점 청크)로 마무리한다. 사이드 브랜치 도중
        // 또 다방향 청크를 만나면 그 나머지 진입점도 대기열에 추가되어 재귀적으로 더 갈라진다.
        private class SideBranchTask
        {
            public Vector2 cursor;
            public int stepBudget;
        }

        private static bool IsDeadEndEntry(ChunkDatabaseEntry entry)
        {
            List<Vector2Int> all = entry.GetAllEntrances();
            for (int i = 1; i < all.Count; i++)
                if (all[i] != all[0]) return false;
            return true; // 모든 진입점이 동일 좌표 -> 입구=출구인 막다른길
        }

        public static void GenerateBranching(ChunkDatabase db, int contentChunkCount, int seed)
        {
            // 구조: Start - (Content(Combat/Puzzle/Treasure) + Transition 반복) - End
            // contentChunkCount는 순수 Content 청크 개수만 센다. Transition은 Content끼리
            // 이어주는 연결용이라 개수에 포함되지 않고, 필요한 만큼 자동으로 끼워 넣는다.
            if (db == null) { Debug.LogError("[ChunkMapGenerator] ChunkDatabase가 없습니다."); return; }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            GameObject root = new GameObject(RootName);

            System.Random rng = new System.Random(seed);
            List<Rect> placedBoxes = new List<Rect>();
            List<PlacementFrame> stack = new List<PlacementFrame>();
            List<SideBranchTask> sideBranches = new List<SideBranchTask>();

            const int MAX_BACKTRACKS = 3000;
            int totalBacktracks = 0;
            int step = 0;           // stack 인덱스(Content+Transition 전부 포함)
            int contentPlaced = 0;  // Content 청크만 센 개수 -> contentChunkCount에서 멈춘다
            int globalIndex = 0;

            Vector2 cursor = new Vector2(0f, 3f);

            // ---- 시작(Start) 청크 배치 ----
            {
                List<ChunkDatabaseEntry> startCandidates = new List<ChunkDatabaseEntry>(db.GetByType(ChunkType.EndLine));
                startCandidates.RemoveAll(e => e.endLineRole != EndLineRole.Start);
                Shuffle(startCandidates, rng);
                foreach (ChunkDatabaseEntry entry in startCandidates)
                {
                    if (!TryConnectBranch(entry, cursor, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor, out _))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_start" + globalIndex++;
                    placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));
                    break;
                }
            }

            // ---- 메인 경로: Content가 contentChunkCount개 놓일 때까지, Transition은 그 사이사이 자동 삽입 ----
            while (contentPlaced < contentChunkCount)
            {
                if (step == stack.Count)
                {
                    bool wantTransition = rng.Next(2) == 0; // 엄격한 교대 대신 무작위 선택(Content-Content, Transition-Transition 연속 허용)
                    ChunkType desiredType = wantTransition ? ChunkType.Transition : RandomContentType(rng);
                    Vector2 cursorBefore = (step == 0) ? cursor : stack[step - 1].cursorAfter;
                    stack.Add(new PlacementFrame { desiredType = desiredType, cursorBefore = cursorBefore });
                }

                PlacementFrame frame = stack[step];
                List<ChunkDatabaseEntry> candidates = new List<ChunkDatabaseEntry>(db.GetByType(frame.desiredType));
                candidates.RemoveAll(e => frame.excluded.Contains(e) || IsDeadEndEntry(e));
                Shuffle(candidates, rng);

                bool placed = false;
                foreach (ChunkDatabaseEntry entry in candidates)
                {
                    if (!TryConnectBranch(entry, frame.cursorBefore, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor, out List<Vector2> unusedWorldPoints))
                        continue;

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_main" + globalIndex++;

                    placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));
                    if (frame.desiredType != ChunkType.Transition) contentPlaced++;

                    frame.placedEntry = entry;
                    frame.instance = inst;
                    frame.cursorAfter = nextCursor;

                    foreach (Vector2 p in unusedWorldPoints)
                        sideBranches.Add(new SideBranchTask { cursor = p, stepBudget = rng.Next(2, 6) });

                    step++;
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    if (step == 0) { Debug.LogError("[ChunkMapGenerator] 첫 스텝부터 배치 가능한 청크가 없습니다 — 생성 중단."); break; }
                    totalBacktracks++;
                    if (totalBacktracks > MAX_BACKTRACKS)
                    {
                        Debug.LogWarning($"[ChunkMapGenerator] 백트래킹 한도({MAX_BACKTRACKS}회) 초과 — Content {contentPlaced}개까지만 배치.");
                        break;
                    }
                    step--;
                    PlacementFrame prevFrame = stack[step];
                    if (prevFrame.instance != null) Object.DestroyImmediate(prevFrame.instance);
                    placedBoxes.RemoveAt(placedBoxes.Count - 1);
                    if (prevFrame.desiredType != ChunkType.Transition && prevFrame.placedEntry != null) contentPlaced--;
                    prevFrame.excluded.Add(prevFrame.placedEntry);
                    prevFrame.placedEntry = null;
                    prevFrame.instance = null;
                    if (stack.Count > step + 1) stack.RemoveRange(step + 1, stack.Count - step - 1);
                }
            }

            Vector2 mainEndCursor = (step > 0) ? stack[step - 1].cursorAfter : cursor;

            // ---- 사이드 브랜치 처리: 여기도 Content 개수만 센다 ----
            int branchGuard = 0;
            while (sideBranches.Count > 0 && branchGuard < 500)
            {
                branchGuard++;
                SideBranchTask task = sideBranches[0];
                sideBranches.RemoveAt(0);

                Vector2 branchCursor = task.cursor;
                int remainingContent = task.stepBudget; // 이 브랜치가 놓을 Content 개수 목표
                int placedInBranch = 0;
                bool capped = false;
                int branchStep = 0;
                int branchFailSafe = 0;

                while (placedInBranch < remainingContent && branchFailSafe < 20)
                {
                    branchFailSafe++;
                    bool wantTransition = rng.Next(2) == 0;
                    ChunkType desiredType = wantTransition ? ChunkType.Transition : RandomContentType(rng);
                    List<ChunkDatabaseEntry> candidates = new List<ChunkDatabaseEntry>(db.GetByType(desiredType));
                    candidates.RemoveAll(e => IsDeadEndEntry(e));
                    Shuffle(candidates, rng);

                    bool placedHere = false;
                    foreach (ChunkDatabaseEntry entry in candidates)
                    {
                        if (!TryConnectBranch(entry, branchCursor, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor, out List<Vector2> unusedWorldPoints))
                            continue;

                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                        inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                        inst.name = entry.prefabName + "_side" + globalIndex++;
                        placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));
                        if (desiredType != ChunkType.Transition) placedInBranch++;

                        foreach (Vector2 p in unusedWorldPoints)
                            sideBranches.Add(new SideBranchTask { cursor = p, stepBudget = rng.Next(2, 6) });

                        branchCursor = nextCursor;
                        branchStep++;
                        placedHere = true;
                        break;
                    }
                    if (!placedHere) break;
                }

                // 사이드 브랜치는 일반 막다른길(EndLineRole.Normal)로 마무리
                List<ChunkDatabaseEntry> deadEndCandidates = new List<ChunkDatabaseEntry>(db.GetByType(ChunkType.EndLine));
                deadEndCandidates.RemoveAll(e => e.endLineRole != EndLineRole.Normal);
                Shuffle(deadEndCandidates, rng);
                foreach (ChunkDatabaseEntry entry in deadEndCandidates)
                {
                    if (!TryConnectBranch(entry, branchCursor, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor, out _))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_cap" + globalIndex++;
                    placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));
                    capped = true;
                    break;
                }
                if (!capped)
                    Debug.LogWarning($"[ChunkMapGenerator] 사이드 브랜치 하나를 막다른길로 마무리하지 못했습니다 (커서={branchCursor}).");
            }

            // ---- 끝(End) 청크 배치: mainEndCursor에서 겹치지 않을 때까지, 필요하면 Transition을
            //      하나 더 끼워서 Start로부터 충분히 멀어지도록 '늘려서' 재시도한다(줄이지 않음) ----
            {
                List<ChunkDatabaseEntry> endCandidates = new List<ChunkDatabaseEntry>(db.GetByType(ChunkType.EndLine));
                endCandidates.RemoveAll(e => e.endLineRole != EndLineRole.End);

                System.Func<Vector2, bool> canPlaceEndAt = (c) =>
                {
                    foreach (ChunkDatabaseEntry entry in endCandidates)
                        if (TryConnectBranch(entry, c, placedBoxes, rng, out Vector2 _o, out Vector2 _n, out _))
                            return true;
                    return false;
                };

                // 확장 이력(되돌리기용). 각 항목에 '이 청크를 놓기 직전 커서'도 같이 저장해서
                // 되돌릴 때 커서를 정확히 복원한다. 막히면 최근 몇 개를 지우고 다시 시도한다.
                List<(GameObject inst, Rect box, bool isContent, Vector2 cursorBefore)> extendHistory
                    = new List<(GameObject, Rect, bool, Vector2)>();
                int extendAttempts = 0;
                const int MAX_EXTEND_ATTEMPTS = 300;
                const int UNDO_ON_STUCK = 3;

                while (!canPlaceEndAt(mainEndCursor) && extendAttempts < MAX_EXTEND_ATTEMPTS)
                {
                    extendAttempts++;
                    ChunkType desiredType = (rng.Next(2) == 0) ? ChunkType.Transition : RandomContentType(rng);
                    List<ChunkDatabaseEntry> candidates = new List<ChunkDatabaseEntry>(db.GetByType(desiredType));
                    candidates.RemoveAll(e => IsDeadEndEntry(e));
                    Shuffle(candidates, rng);

                    bool extended = false;
                    foreach (ChunkDatabaseEntry entry in candidates)
                    {
                        if (!TryConnectBranch(entry, mainEndCursor, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor, out List<Vector2> unusedWorldPoints))
                            continue;
                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                        inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                        inst.name = entry.prefabName + "_extend" + globalIndex++;
                        Rect box = new Rect(origin.x, origin.y, entry.width, entry.height);
                        placedBoxes.Add(box);
                        bool isContent = desiredType != ChunkType.Transition;
                        if (isContent) contentPlaced++;
                        extendHistory.Add((inst, box, isContent, mainEndCursor));
                        mainEndCursor = nextCursor;
                        extended = true;
                        break;
                    }

                    if (!extended)
                    {
                        int undoCount = Mathf.Min(UNDO_ON_STUCK, extendHistory.Count);
                        if (undoCount > 0)
                        {
                            for (int u = 0; u < undoCount; u++)
                            {
                                var last = extendHistory[extendHistory.Count - 1];
                                extendHistory.RemoveAt(extendHistory.Count - 1);
                                Object.DestroyImmediate(last.inst);
                                placedBoxes.Remove(last.box);
                                if (last.isContent) contentPlaced--;
                                mainEndCursor = last.cursorBefore;
                            }
                        }
                        else if (step > 1)
                        {
                            // extend 이력이 없으면(막 시작한 지점부터 막힘) 메인 스택의 마지막
                            // 청크까지 되돌려서 다시 시도한다(더 근본적으로 경로를 바꿔봄).
                            step--;
                            PlacementFrame pf = stack[step];
                            if (pf.placedEntry != null && pf.instance != null)
                            {
                                Vector3 pos = pf.instance.transform.position;
                                Rect pfBox = new Rect(pos.x, pos.y, pf.placedEntry.width, pf.placedEntry.height);
                                placedBoxes.Remove(pfBox);
                                if (pf.placedEntry.type != ChunkType.Transition) contentPlaced--;
                                Object.DestroyImmediate(pf.instance);
                            }
                            pf.excluded.Add(pf.placedEntry);
                            pf.placedEntry = null;
                            pf.instance = null;
                            if (stack.Count > step + 1) stack.RemoveRange(step + 1, stack.Count - step - 1);
                            mainEndCursor = (step > 0) ? stack[step - 1].cursorAfter : cursor;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                step += extendHistory.Count;

                Shuffle(endCandidates, rng);
                bool placedEnd = false;
                foreach (ChunkDatabaseEntry entry in endCandidates)
                {
                    if (!TryConnectBranch(entry, mainEndCursor, placedBoxes, rng, out Vector2 origin, out Vector2 nextCursor, out _))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_end" + globalIndex++;
                    placedBoxes.Add(new Rect(origin.x, origin.y, entry.width, entry.height));
                    placedEnd = true;
                    break;
                }
                if (!placedEnd)
                    Debug.LogWarning("[ChunkMapGenerator] End 청크를 배치하지 못했습니다 (EndLineRole.End 프리팹이 없거나 겹침).");
            }

            Debug.Log($"[ChunkMapGenerator] 가지치기 생성 완료: Content {contentPlaced}개(목표 {contentChunkCount}), 사이드 브랜치 처리 {branchGuard}개. seed={seed}");
        }


        // TryConnect과 동일하지만, 사용하지 않은 나머지 진입점들의 월드 좌표도 함께 반환한다
        // (다방향 청크를 만났을 때 사이드 브랜치 시작점으로 쓰기 위함).
        private static bool TryConnectBranch(ChunkDatabaseEntry entry, Vector2 cursor, List<Rect> placedBoxes,
            System.Random rng, out Vector2 origin, out Vector2 nextCursor, out List<Vector2> unusedWorldPoints)
        {
            List<Vector2Int> entrances = entry.GetAllEntrances();
            List<int> order = new List<int>();
            for (int i = 0; i < entrances.Count; i++) order.Add(i);
            Shuffle(order, rng);

            foreach (int inIdx in order)
            {
                Vector2Int inSocket = entrances[inIdx];
                Vector2 candidateOrigin = cursor - new Vector2(inSocket.x, inSocket.y);
                Rect box = new Rect(candidateOrigin.x, candidateOrigin.y, entry.width, entry.height);
                if (Overlaps(box, placedBoxes)) continue;

                List<int> otherIdx = new List<int>();
                for (int i = 0; i < entrances.Count; i++) if (i != inIdx) otherIdx.Add(i);
                if (otherIdx.Count == 0) continue;

                int outIdx = otherIdx[rng.Next(otherIdx.Count)];
                Vector2Int outSocket = entrances[outIdx];

                origin = candidateOrigin;
                nextCursor = candidateOrigin + new Vector2(outSocket.x, outSocket.y);

                unusedWorldPoints = new List<Vector2>();
                for (int i = 0; i < otherIdx.Count; i++)
                {
                    if (otherIdx[i] == outIdx) continue;
                    Vector2Int p = entrances[otherIdx[i]];
                    unusedWorldPoints.Add(candidateOrigin + new Vector2(p.x, p.y));
                }
                return true;
            }

            origin = Vector2.zero;
            nextCursor = Vector2.zero;
            unusedWorldPoints = new List<Vector2>();
            return false;
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
