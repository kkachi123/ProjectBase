using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MapSystem.Editor.ChunkPlacementUtility;

namespace MapSystem.Editor
{
    // ================= 가지치기(branching) 맵 생성 =================
    // 메인 경로는 선형 생성기(백트래킹 포함)와 동일하게 진행하되, 다방향(3개 이상
    // 진입점) 청크를 만나면 실제 사용하지 않은 나머지 진입점들을 '사이드 브랜치 대기열'에
    // 넣어둔다. 메인 경로가 끝나면 대기열을 순서대로 처리해서, 각 사이드 브랜치를 무작위
    // 스텝(2~5) 진행한 뒤 막다른길(단일 진입점 청크)로 마무리한다. 사이드 브랜치 도중
    // 또 다방향 청크를 만나면 그 나머지 진입점도 대기열에 추가되어 재귀적으로 더 갈라진다.
    internal static class ChunkBranchingGenerator
    {
        private class SideBranchTask
        {
            public Vector2 cursor;
            public int stepBudget;
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
    }
}
