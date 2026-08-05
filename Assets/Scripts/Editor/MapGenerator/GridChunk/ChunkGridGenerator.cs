using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static GridMapSystem.Editor.GridPlacementUtility;

namespace GridMapSystem.Editor
{
    // ================= 그리드 기반 맵 생성(방식 2) =================
    // ChunkBranchingGenerator(방식 1)와 비슷한 단계 구성(Start 배치 -> 메인 경로+사이드 브랜치
    // 수집 -> 사이드 브랜치 소진 -> End 배치)을 따르되, 겹침 검사를 픽셀 Rect 대신 "셀 점유
    // 여부"(HashSet<Vector2Int>)로 하고, Start/막다른길 캡/End는 진입점이 1개뿐이어도
    // 성립하는 TryConnectGridTerminal을 쓴다(방식 1은 이 경우 항상 실패하는 문제가 있어
    // 그대로 포팅하지 않았다).
    //
    // 메인 경로 개수는 청크를 추가로 늘려서 맞추지 않는다 — Content를 정확히
    // contentChunkCount개만 놓고, 그 시점에 End가 안 붙으면 마지막 청크(들)를 다른 후보로
    // 교체해가며(백트래킹) End가 붙는 조합을 찾는다. 그래서 최종 Content 개수는 항상
    // contentChunkCount와 같다(방식 1의 "확장(extend)" 방식과 다른 점).
    public static class ChunkGridGenerator
    {
        private class SideBranchTask
        {
            public Vector2Int cursor;
            public int stepBudget;
        }

        // 방금 배치한 청크 인스턴스의 GridChunkData.GetResolvedSpawns()를 읽어서, SpawnKind별로
        // 매핑된 프리팹을 그 자식으로 심는다. 매핑이 비어있는 종류(아직 프리팹이 없는 경우)는
        // 조용히 건너뛴다.
        private static void SpawnContentFor(GameObject chunkInstance, System.Random rng, SpawnPrefabSet spawnPrefabs)
        {
            GridChunkData data = chunkInstance.GetComponent<GridChunkData>();
            if (data == null) return;

            int index = 0;
            foreach (var (position, kind) in data.GetResolvedSpawns(rng))
            {
                GameObject prefab = spawnPrefabs.Get(kind);
                if (prefab == null) continue;

                GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chunkInstance.transform);
                spawned.transform.position = chunkInstance.transform.position + new Vector3(position.x, position.y, 0f);
                spawned.name = $"{kind}_{index++}";
            }
        }

        // Start/End 청크 전용 — 실제 생성/처리는 하지 않고, 그 청크의 스폰 포인트(1개) 월드
        // 좌표만 뽑아낸다. 실제 Player 생성/종료 처리는 나중에 런타임 GameManager가
        // GeneratedMapInfo에서 이 좌표를 읽어서 알아서 한다.
        private static bool TryGetSpawnWorldPosition(GameObject chunkInstance, out Vector3 worldPosition)
        {
            GridChunkData data = chunkInstance.GetComponent<GridChunkData>();
            if (data == null) { worldPosition = default; return false; }

            List<Vector2Int> points = data.GetSpawnPoints();
            if (points.Count == 0) { worldPosition = default; return false; }

            worldPosition = chunkInstance.transform.position + new Vector3(points[0].x, points[0].y, 0f);
            return true;
        }

        public static void GenerateGrid(GridChunkDatabase db, int contentChunkCount, int seed, SpawnPrefabSet spawnPrefabs = null)
        {
            spawnPrefabs = spawnPrefabs ?? new SpawnPrefabSet();
            if (db == null) { Debug.LogError("[ChunkGridGenerator] GridChunkDatabase가 없습니다."); return; }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            GameObject root = new GameObject(RootName);

            // GameManager 등 런타임 쪽에서 읽어갈 위치 정보. 실제 Player 생성/종료 처리는 여기서 안 함.
            Vector3 playerSpawnPosition = Vector3.zero;
            Vector3 endPosition = Vector3.zero;

            System.Random rng = new System.Random(seed);
            // 점유된 셀 좌표(10 단위 격자) 기록
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>(); 
            // 백트래킹 스택. 각 스텝마다 배치 시도 기록을 남기고, 실패 시 되돌린다.
            List<GridPlacementFrame> stack = new List<GridPlacementFrame>();
            // 사이드 브랜치 후보 커서와 남은 스텝 수를 기록. 
            // 메인 경로 배치 중에 발견되면 여기에 추가하고, 메인 경로가 끝난 뒤에 처리한다. 
            List<SideBranchTask> sideBranches = new List<SideBranchTask>(); 

            const int MAX_BACKTRACKS = 3000;
            int totalBacktracks = 0;
            int step = 0;
            int contentPlaced = 0;
            int globalIndex = 0;

            Vector2Int cursor = new Vector2Int(0, 0);

            // ---- 시작(Start) 청크 배치 ----
            // 원점(0,0)에 배치 -> 그 청크의 입구 좌표를 다음 커서로.
            {
                // EndLineRole.Start로 지정된 전용 청크 중에서만 고른다.
                List<GridChunkDatabaseEntry> startCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
                startCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.Start);
                Shuffle(startCandidates, rng);
                // 빈 진입점 방어. 휴먼에러 방지(진입점 0개짜리 존재 시 다음 Chunk 연결 불가능하므로).
                foreach (GridChunkDatabaseEntry entry in startCandidates)
                {
                    List<Vector2Int> entrances = entry.GetAllEntrances();
                    if (entrances.Count == 0) continue;
                    
                    Vector2Int origin = Vector2Int.zero;
                    Vector2Int chosenEntrance = entrances[0]; // EndLine은 진입점 1개만 설정하므로 무작위 선택 불필요
                    List<Vector2Int> claimedCells = OccupiedCellsFor(origin, entry.footprint);

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_start" + globalIndex++;
                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    TryGetSpawnWorldPosition(inst, out playerSpawnPosition);

                    cursor = origin + chosenEntrance;
                    break;
                }
            }

            // End 후보/판정 함수를 메인 경로 루프보다 먼저 준비해둔다 — 목표 개수에 도달했을 때
            // "여기서 바로 End를 붙일 수 있는가"까지 같이 확인해야 하기 때문.
            List<GridChunkDatabaseEntry> endCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
            endCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.End);
            System.Func<Vector2Int, bool> canPlaceEndAt = (c) =>
            {
                foreach (GridChunkDatabaseEntry entry in endCandidates)
                    if (TryConnectGridTerminal(entry, c, occupiedCells, rng, out Vector2Int _o, out List<Vector2Int> _cells))
                        return true;
                return false;
            };

            // ---- 메인 경로: Content를 정확히 contentChunkCount개만 놓는다(더 늘리지 않음).
            // 목표 개수를 채운 시점에 End가 바로 안 붙으면, 청크를 새로 추가하는 대신 마지막
            // 청크를 다른 후보로 교체해가며(필요하면 그 앞 칸까지 계속 되돌아가며) End가 붙는
            // 조합을 찾는다 — 그래서 최종 Content 개수는 항상 contentChunkCount와 같다.
            while (true)
            {
                Vector2Int cursorSoFar = (step > 0) ? stack[step - 1].cursorAfter : cursor;

                if (contentPlaced >= contentChunkCount)
                {
                    if (canPlaceEndAt(cursorSoFar)) break; // 목표 개수 + End 연결 가능 -> 메인 경로 완성

                    // 새 칸을 추가하지 않고, 마지막 칸을 되돌려서 다른 후보로 재시도한다.
                    if (step == 0)
                    {
                        Debug.LogWarning("[ChunkGridGenerator] End가 붙는 조합을 찾지 못했습니다 — Content 0개로는 시작 지점 자체가 End와 안 맞습니다.");
                        break;
                    }
                    totalBacktracks++;
                    if (totalBacktracks > MAX_BACKTRACKS)
                    {
                        Debug.LogWarning($"[ChunkGridGenerator] End가 붙는 조합을 백트래킹 한도({MAX_BACKTRACKS}회) 안에 찾지 못했습니다 — 이 상태로 종료합니다.");
                        break;
                    }
                    step--;
                    GridPlacementFrame endRetryFrame = stack[step];
                    if (endRetryFrame.instance != null) Object.DestroyImmediate(endRetryFrame.instance);
                    if (endRetryFrame.claimedCells != null)
                        foreach (var c in endRetryFrame.claimedCells) occupiedCells.Remove(c);
                    contentPlaced--; // desiredType은 항상 Content
                    endRetryFrame.excluded.Add(endRetryFrame.placedEntry);
                    endRetryFrame.placedEntry = null;
                    endRetryFrame.instance = null;
                    endRetryFrame.claimedCells = null;
                    if (stack.Count > step + 1) stack.RemoveRange(step + 1, stack.Count - step - 1);
                    continue; // 위로 돌아가서 이번엔 이 칸을 일반 로직으로 다시 채움
                }

                if (step == stack.Count)
                {
                    GridChunkType desiredType = GridChunkType.Content;
                    stack.Add(new GridPlacementFrame { desiredType = desiredType, cursorBefore = cursorSoFar });
                }

                GridPlacementFrame frame = stack[step];
                List<GridChunkDatabaseEntry> candidates = new List<GridChunkDatabaseEntry>(db.GetByType(frame.desiredType));
                candidates.RemoveAll(e => frame.excluded.Contains(e) || IsDeadEndEntry(e));
                Shuffle(candidates, rng);

                bool placed = false;
                foreach (GridChunkDatabaseEntry entry in candidates)
                {
                    if (!TryConnectBranchGrid(entry, frame.cursorBefore, occupiedCells, rng, out Vector2Int origin, out Vector2Int nextCursor,
                            out List<Vector2Int> claimedCells, out List<Vector2Int> unusedWorldPoints))
                        continue;

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_main" + globalIndex++;
                    SpawnContentFor(inst, rng, spawnPrefabs);

                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    if (frame.desiredType != GridChunkType.Transition) contentPlaced++;

                    frame.placedEntry = entry;
                    frame.instance = inst;
                    frame.cursorAfter = nextCursor;
                    frame.claimedCells = claimedCells;

                    foreach (Vector2Int p in unusedWorldPoints)
                        sideBranches.Add(new SideBranchTask { cursor = p, stepBudget = rng.Next(2, 6) });

                    step++;
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    if (step == 0) { Debug.LogError("[ChunkGridGenerator] 첫 스텝부터 배치 가능한 청크가 없습니다 — 생성 중단."); break; }
                    totalBacktracks++;
                    if (totalBacktracks > MAX_BACKTRACKS)
                    {
                        Debug.LogWarning($"[ChunkGridGenerator] 백트래킹 한도({MAX_BACKTRACKS}회) 초과 — Content {contentPlaced}개까지만 배치.");
                        break;
                    }
                    step--;
                    GridPlacementFrame prevFrame = stack[step];
                    if (prevFrame.instance != null) Object.DestroyImmediate(prevFrame.instance);
                    if (prevFrame.claimedCells != null)
                        foreach (var c in prevFrame.claimedCells) occupiedCells.Remove(c);
                    if (prevFrame.desiredType != GridChunkType.Transition && prevFrame.placedEntry != null) contentPlaced--;
                    prevFrame.excluded.Add(prevFrame.placedEntry);
                    prevFrame.placedEntry = null;
                    prevFrame.instance = null;
                    prevFrame.claimedCells = null;
                    if (stack.Count > step + 1) stack.RemoveRange(step + 1, stack.Count - step - 1);
                }
            }

            Vector2Int mainEndCursor = (step > 0) ? stack[step - 1].cursorAfter : cursor;

            // ---- 사이드 브랜치 처리: 여기도 Content 개수만 센다 ----
            int branchGuard = 0;
            while (sideBranches.Count > 0 && branchGuard < 500)
            {
                branchGuard++;
                SideBranchTask task = sideBranches[0];
                sideBranches.RemoveAt(0);

                Vector2Int branchCursor = task.cursor;
                int remainingContent = task.stepBudget;
                int placedInBranch = 0;
                bool capped = false;
                int branchFailSafe = 0;

                while (placedInBranch < remainingContent && branchFailSafe < 20)
                {
                    branchFailSafe++;
                    GridChunkType desiredType = GridChunkType.Content;
                    List<GridChunkDatabaseEntry> candidates = new List<GridChunkDatabaseEntry>(db.GetByType(desiredType));
                    candidates.RemoveAll(e => IsDeadEndEntry(e));
                    Shuffle(candidates, rng);

                    bool placedHere = false;
                    foreach (GridChunkDatabaseEntry entry in candidates)
                    {
                        if (!TryConnectBranchGrid(entry, branchCursor, occupiedCells, rng, out Vector2Int origin, out Vector2Int nextCursor,
                                out List<Vector2Int> claimedCells, out List<Vector2Int> unusedWorldPoints))
                            continue;

                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                        inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                        inst.name = entry.prefabName + "_side" + globalIndex++;
                        SpawnContentFor(inst, rng, spawnPrefabs);
                        foreach (var c in claimedCells) occupiedCells.Add(c);
                        if (desiredType != GridChunkType.Transition) placedInBranch++;

                        foreach (Vector2Int p in unusedWorldPoints)
                            sideBranches.Add(new SideBranchTask { cursor = p, stepBudget = rng.Next(2, 6) });

                        branchCursor = nextCursor;
                        placedHere = true;
                        break;
                    }
                    if (!placedHere) break;
                }

                // 사이드 브랜치는 일반 막다른길(EndLineRole.Normal)로 마무리
                List<GridChunkDatabaseEntry> deadEndCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
                deadEndCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.Normal);
                Shuffle(deadEndCandidates, rng);
                foreach (GridChunkDatabaseEntry entry in deadEndCandidates)
                {
                    if (!TryConnectGridTerminal(entry, branchCursor, occupiedCells, rng, out Vector2Int origin, out List<Vector2Int> claimedCells))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_cap" + globalIndex++;
                    SpawnContentFor(inst, rng, spawnPrefabs);
                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    capped = true;
                    break;
                }
                if (!capped)
                    Debug.LogWarning($"[ChunkGridGenerator] 사이드 브랜치 하나를 막다른길로 마무리하지 못했습니다 (커서={branchCursor}).");
            }

            // ---- 끝(End) 청크 배치: 메인 경로 루프가 이미 "여기서 End가 붙는다"를 확인하고
            //      끝났으므로, 여기서는 그 확인된 자리에 실제로 심기만 하면 된다. ----
            {
                Shuffle(endCandidates, rng);
                bool placedEnd = false;
                foreach (GridChunkDatabaseEntry entry in endCandidates)
                {
                    if (!TryConnectGridTerminal(entry, mainEndCursor, occupiedCells, rng, out Vector2Int origin, out List<Vector2Int> claimedCells))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_end" + globalIndex++;
                    TryGetSpawnWorldPosition(inst, out endPosition);
                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    placedEnd = true;
                    break;
                }
                if (!placedEnd)
                    Debug.LogWarning("[ChunkGridGenerator] End 청크를 배치하지 못했습니다 (EndLine 프리팹이 없거나 자리가 없음, 또는 메인 경로에서 End 연결 가능 지점을 못 찾고 포기함).");
            }

            GeneratedMapInfo info = root.AddComponent<GeneratedMapInfo>();
            info.playerSpawnPosition = playerSpawnPosition;
            info.endPosition = endPosition;

            Debug.Log($"[ChunkGridGenerator] 그리드 생성 완료: Content {contentPlaced}개(목표 {contentChunkCount}), 사이드 브랜치 처리 {branchGuard}개. seed={seed}");
        }
    }
}
