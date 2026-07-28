using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static GridMapSystem.Editor.GridPlacementUtility;

namespace GridMapSystem.Editor
{
    // ================= 그리드 기반 맵 생성(방식 2) =================
    // ChunkBranchingGenerator(방식 1)와 동일한 5단계 구조(Start 배치 -> 메인 경로+사이드 브랜치
    // 수집 -> 사이드 브랜치 소진 -> End 배치+확장/백트래킹)를 그대로 따르되, 겹침 검사를 픽셀
    // Rect 대신 "셀 점유 여부"(HashSet<Vector2Int>)로 하고, Start/막다른길 캡/End는 진입점이
    // 1개뿐이어도 성립하는 TryConnectGridTerminal을 쓴다(방식 1은 이 경우 항상 실패하는
    // 문제가 있어 그대로 포팅하지 않았다).
    public static class ChunkGridGenerator
    {
        private class SideBranchTask
        {
            public Vector2Int cursor;
            public int stepBudget;
        }

        public static void GenerateGrid(GridChunkDatabase db, int contentChunkCount, int seed)
        {
            // 구조: Start - (Content(Combat/Puzzle/Treasure) + Transition 반복) - End
            // contentChunkCount는 순수 Content 청크 개수만 센다. Transition은 Content끼리
            // 이어주는 연결용이라 개수에 포함되지 않고, 필요한 만큼 자동으로 끼워 넣는다.
            if (db == null) { Debug.LogError("[ChunkGridGenerator] GridChunkDatabase가 없습니다."); return; }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            GameObject root = new GameObject(RootName);

            System.Random rng = new System.Random(seed);
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
            List<GridPlacementFrame> stack = new List<GridPlacementFrame>();
            List<SideBranchTask> sideBranches = new List<SideBranchTask>();

            const int MAX_BACKTRACKS = 3000;
            int totalBacktracks = 0;
            int step = 0;
            int contentPlaced = 0;
            int globalIndex = 0;

            Vector2Int cursor = new Vector2Int(0, 0);

            // ---- 시작(Start) 청크 배치 ----
            {
                List<GridChunkDatabaseEntry> startCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
                startCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.Start);
                Shuffle(startCandidates, rng);
                foreach (GridChunkDatabaseEntry entry in startCandidates)
                {
                    if (!TryConnectGridTerminal(entry, cursor, occupiedCells, rng, out Vector2Int origin, out List<Vector2Int> claimedCells))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_start" + globalIndex++;
                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    break;
                }
            }

            // ---- 메인 경로: Content가 contentChunkCount개 놓일 때까지, Transition은 그 사이사이 자동 삽입 ----
            while (contentPlaced < contentChunkCount)
            {
                if (step == stack.Count)
                {
                    bool wantTransition = rng.Next(2) == 0;
                    GridChunkType desiredType = wantTransition ? GridChunkType.Transition : RandomContentType(rng);
                    Vector2Int cursorBefore = (step == 0) ? cursor : stack[step - 1].cursorAfter;
                    stack.Add(new GridPlacementFrame { desiredType = desiredType, cursorBefore = cursorBefore });
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
                    bool wantTransition = rng.Next(2) == 0;
                    GridChunkType desiredType = wantTransition ? GridChunkType.Transition : RandomContentType(rng);
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

                // 사이드 브랜치는 일반 막다른길(GridEndLineRole.Normal)로 마무리
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
                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    capped = true;
                    break;
                }
                if (!capped)
                    Debug.LogWarning($"[ChunkGridGenerator] 사이드 브랜치 하나를 막다른길로 마무리하지 못했습니다 (커서={branchCursor}).");
            }

            // ---- 끝(End) 청크 배치: mainEndCursor에서 배치될 때까지, 필요하면 Transition을
            //      하나 더 끼워서 Start로부터 충분히 멀어지도록 '늘려서' 재시도한다(줄이지 않음) ----
            {
                List<GridChunkDatabaseEntry> endCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
                endCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.End);

                System.Func<Vector2Int, bool> canPlaceEndAt = (c) =>
                {
                    foreach (GridChunkDatabaseEntry entry in endCandidates)
                        if (TryConnectGridTerminal(entry, c, occupiedCells, rng, out Vector2Int _o, out List<Vector2Int> _cells))
                            return true;
                    return false;
                };

                List<(GameObject inst, List<Vector2Int> cells, bool isContent, Vector2Int cursorBefore)> extendHistory
                    = new List<(GameObject, List<Vector2Int>, bool, Vector2Int)>();
                int extendAttempts = 0;
                const int MAX_EXTEND_ATTEMPTS = 300;
                const int UNDO_ON_STUCK = 3;

                while (!canPlaceEndAt(mainEndCursor) && extendAttempts < MAX_EXTEND_ATTEMPTS)
                {
                    extendAttempts++;
                    GridChunkType desiredType = (rng.Next(2) == 0) ? GridChunkType.Transition : RandomContentType(rng);
                    List<GridChunkDatabaseEntry> candidates = new List<GridChunkDatabaseEntry>(db.GetByType(desiredType));
                    candidates.RemoveAll(e => IsDeadEndEntry(e));
                    Shuffle(candidates, rng);

                    bool extended = false;
                    foreach (GridChunkDatabaseEntry entry in candidates)
                    {
                        if (!TryConnectBranchGrid(entry, mainEndCursor, occupiedCells, rng, out Vector2Int origin, out Vector2Int nextCursor,
                                out List<Vector2Int> claimedCells, out List<Vector2Int> unusedWorldPoints))
                            continue;
                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                        inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                        inst.name = entry.prefabName + "_extend" + globalIndex++;
                        foreach (var c in claimedCells) occupiedCells.Add(c);
                        bool isContent = desiredType != GridChunkType.Transition;
                        if (isContent) contentPlaced++;
                        extendHistory.Add((inst, claimedCells, isContent, mainEndCursor));
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
                                foreach (var c in last.cells) occupiedCells.Remove(c);
                                if (last.isContent) contentPlaced--;
                                mainEndCursor = last.cursorBefore;
                            }
                        }
                        else if (step > 1)
                        {
                            step--;
                            GridPlacementFrame pf = stack[step];
                            if (pf.placedEntry != null && pf.instance != null)
                            {
                                if (pf.claimedCells != null)
                                    foreach (var c in pf.claimedCells) occupiedCells.Remove(c);
                                if (pf.placedEntry.type != GridChunkType.Transition) contentPlaced--;
                                Object.DestroyImmediate(pf.instance);
                            }
                            pf.excluded.Add(pf.placedEntry);
                            pf.placedEntry = null;
                            pf.instance = null;
                            pf.claimedCells = null;
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
                foreach (GridChunkDatabaseEntry entry in endCandidates)
                {
                    if (!TryConnectGridTerminal(entry, mainEndCursor, occupiedCells, rng, out Vector2Int origin, out List<Vector2Int> claimedCells))
                        continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_end" + globalIndex++;
                    foreach (var c in claimedCells) occupiedCells.Add(c);
                    placedEnd = true;
                    break;
                }
                if (!placedEnd)
                    Debug.LogWarning("[ChunkGridGenerator] End 청크를 배치하지 못했습니다 (GridEndLineRole.End 프리팹이 없거나 자리가 없음).");
            }

            Debug.Log($"[ChunkGridGenerator] 그리드 생성 완료: Content {contentPlaced}개(목표 {contentChunkCount}), 사이드 브랜치 처리 {branchGuard}개. seed={seed}");
        }
    }
}
