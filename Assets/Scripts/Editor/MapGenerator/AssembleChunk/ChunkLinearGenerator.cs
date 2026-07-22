using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MapSystem.Editor.ChunkPlacementUtility;

namespace MapSystem.Editor
{
    // 선형 조합 생성기 — 다방향 분기 없이 백트래킹만으로 청크를 순서대로 이어붙인다.
    internal static class ChunkLinearGenerator
    {
        // 기능 : 청크를 steps개 이어붙여 배치한다. 실패하면 가능한 만큼만 배치하고 중단한다.
        // 막히면 그냥 스킵하는 대신, 이전 스텝으로 돌아가 다른 청크를 다시 골라본다(백트래킹).
        // 이전 스텝도 더 이상 대안이 없으면 그보다 더 앞으로 계속 되돌아간다.
        public static void Generate(ChunkDatabase db, int steps, int seed)
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
            // 일단 커서만 이동, 청크 배치가 안 되면 이전 스텝으로 돌아가 다른 청크를 시도하는 백트래킹 방식.
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
    }
}
