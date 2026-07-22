using System.Collections.Generic;
using UnityEngine;

namespace MapSystem.Editor
{
    // ChunkLinearGenerator / ChunkBranchingGenerator가 공유하는 저수준 배치·연결 유틸리티.
    internal static class ChunkPlacementUtility
    {
        internal const string RootName = "GeneratedMap";

        // 한 스텝의 배치 시도 기록 -- 백트래킹 시 이 프레임 단위로 되돌린다.
        internal class PlacementFrame
        {
            public ChunkType desiredType;
            public Vector2 cursorBefore;
            public HashSet<ChunkDatabaseEntry> excluded = new HashSet<ChunkDatabaseEntry>();
            public ChunkDatabaseEntry placedEntry;
            public GameObject instance;
            public Vector2 cursorAfter;
        }

        // 후보 하나를 커서에 연결한다. entrances 목록(N개) 중 하나를 입구로 삼아 시도하고,
        // 배치가 겹치지 않으면 나머지 진입점 중 하나를 무작위로 골라 출구(다음 커서)로 사용한다.
        // 진입점이 2개뿐인 기존 청크는 이전과 완전히 동일하게 동작한다(둘 중 하나가 입구면
        // 나머지 하나가 자동으로 출구가 됨).
        internal static bool TryConnect(ChunkDatabaseEntry entry, Vector2 cursor, List<Rect> placedBoxes,
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

        // TryConnect과 동일하지만, 사용하지 않은 나머지 진입점들의 월드 좌표도 함께 반환한다
        // (다방향 청크를 만났을 때 사이드 브랜치 시작점으로 쓰기 위함).
        internal static bool TryConnectBranch(ChunkDatabaseEntry entry, Vector2 cursor, List<Rect> placedBoxes,
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

        internal static bool IsDeadEndEntry(ChunkDatabaseEntry entry)
        {
            List<Vector2Int> all = entry.GetAllEntrances();
            for (int i = 1; i < all.Count; i++)
                if (all[i] != all[0]) return false;
            return true; // 모든 진입점이 동일 좌표 -> 입구=출구인 막다른길
        }

        internal static ChunkType RandomContentType(System.Random rng)
        {
            ChunkType[] options = { ChunkType.Combat, ChunkType.Puzzle, ChunkType.Treasure };
            return options[rng.Next(options.Length)];
        }

        internal static bool Overlaps(Rect box, List<Rect> others)
        {
            foreach (Rect o in others)
                if (box.Overlaps(o)) return true;
            return false;
        }

        internal static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
