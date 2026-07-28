using System.Collections.Generic;
using UnityEngine;

namespace GridMapSystem.Editor
{
    // ChunkGridGenerator가 공유하는 저수준 배치·연결 유틸리티. 기존 방식 1(ChunkPlacementUtility,
    // 픽셀 Rect 겹침 검사)과 완전히 독립된 구현이다 — 겹침 검사를 "셀 점유 여부"로 대체한 것이
    // 핵심 차이이고, 나머지 셔플/백트래킹 개념은 동일하게 가져왔다.
    internal static class GridPlacementUtility
    {
        internal const string RootName = "GeneratedGridMap";

        // 한 스텝의 배치 시도 기록 -- 백트래킹 시 이 프레임 단위로 되돌린다.
        // claimedCells는 이 프레임이 실제로 점유한 칸들(되돌릴 때 정확히 이 칸들만 해제하기 위함).
        internal class GridPlacementFrame
        {
            public GridChunkType desiredType;
            public Vector2Int cursorBefore;
            public HashSet<GridChunkDatabaseEntry> excluded = new HashSet<GridChunkDatabaseEntry>();
            public GridChunkDatabaseEntry placedEntry;
            public GameObject instance;
            public Vector2Int cursorAfter;
            public List<Vector2Int> claimedCells;
        }

        private static List<Vector2Int> OccupiedCellsFor(Vector2Int origin, Vector2Int footprint)
        {
            int baseX = origin.x / GridChunkData.CellSize;
            int baseY = origin.y / GridChunkData.CellSize;
            var cells = new List<Vector2Int>();
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    cells.Add(new Vector2Int(baseX + x, baseY + y));
            return cells;
        }

        private static bool CellsConflict(List<Vector2Int> cells, HashSet<Vector2Int> occupied)
        {
            foreach (var c in cells)
                if (occupied.Contains(c)) return true;
            return false;
        }

        // 후보 하나를 커서에 연결한다(통과 연결 — 입구+출구 둘 다 필요). entrances 목록 중 하나를
        // 입구로 삼아 시도하고, (1) 원점이 셀 격자(10 단위)에 정확히 맞아떨어지는지, (2) 점유된
        // 칸과 안 겹치는지 확인한 뒤, 나머지 진입점 중 하나를 무작위로 골라 출구(다음 커서)로
        // 쓴다. 진입점이 1개뿐이면(막다른길) 통과 연결 후보에서 제외한다.
        internal static bool TryConnectGrid(GridChunkDatabaseEntry entry, Vector2Int cursor, HashSet<Vector2Int> occupied,
            System.Random rng, out Vector2Int origin, out Vector2Int nextCursor, out List<Vector2Int> claimedCells)
        {
            List<Vector2Int> entrances = entry.GetAllEntrances();
            List<int> order = new List<int>();
            for (int i = 0; i < entrances.Count; i++) order.Add(i);
            Shuffle(order, rng);

            foreach (int inIdx in order)
            {
                Vector2Int inSocket = entrances[inIdx];
                Vector2Int candidateOrigin = cursor - inSocket;

                if (candidateOrigin.x % GridChunkData.CellSize != 0 || candidateOrigin.y % GridChunkData.CellSize != 0)
                    continue; // 정렬 가드: 셀 격자에 안 맞으면 후보 제외 (불일치 슬롯 쌍 청크 등)

                List<Vector2Int> cells = OccupiedCellsFor(candidateOrigin, entry.footprint);
                if (CellsConflict(cells, occupied)) continue;

                List<int> otherIdx = new List<int>();
                for (int i = 0; i < entrances.Count; i++) if (i != inIdx) otherIdx.Add(i);
                if (otherIdx.Count == 0) continue;

                int outIdx = otherIdx[rng.Next(otherIdx.Count)];
                Vector2Int outSocket = entrances[outIdx];

                origin = candidateOrigin;
                nextCursor = candidateOrigin + outSocket;
                claimedCells = cells;
                return true;
            }

            origin = Vector2Int.zero;
            nextCursor = Vector2Int.zero;
            claimedCells = null;
            return false;
        }

        // TryConnectGrid과 동일하지만, 사용하지 않은 나머지 진입점들의 월드 좌표도 함께 반환한다
        // (다방향 청크를 만났을 때 사이드 브랜치 시작점으로 쓰기 위함).
        internal static bool TryConnectBranchGrid(GridChunkDatabaseEntry entry, Vector2Int cursor, HashSet<Vector2Int> occupied,
            System.Random rng, out Vector2Int origin, out Vector2Int nextCursor, out List<Vector2Int> claimedCells,
            out List<Vector2Int> unusedWorldPoints)
        {
            List<Vector2Int> entrances = entry.GetAllEntrances();
            List<int> order = new List<int>();
            for (int i = 0; i < entrances.Count; i++) order.Add(i);
            Shuffle(order, rng);

            foreach (int inIdx in order)
            {
                Vector2Int inSocket = entrances[inIdx];
                Vector2Int candidateOrigin = cursor - inSocket;

                if (candidateOrigin.x % GridChunkData.CellSize != 0 || candidateOrigin.y % GridChunkData.CellSize != 0)
                    continue;

                List<Vector2Int> cells = OccupiedCellsFor(candidateOrigin, entry.footprint);
                if (CellsConflict(cells, occupied)) continue;

                List<int> otherIdx = new List<int>();
                for (int i = 0; i < entrances.Count; i++) if (i != inIdx) otherIdx.Add(i);
                if (otherIdx.Count == 0) continue;

                int outIdx = otherIdx[rng.Next(otherIdx.Count)];
                Vector2Int outSocket = entrances[outIdx];

                origin = candidateOrigin;
                nextCursor = candidateOrigin + outSocket;
                claimedCells = cells;

                unusedWorldPoints = new List<Vector2Int>();
                for (int i = 0; i < otherIdx.Count; i++)
                {
                    if (otherIdx[i] == outIdx) continue;
                    unusedWorldPoints.Add(candidateOrigin + entrances[otherIdx[i]]);
                }
                return true;
            }

            origin = Vector2Int.zero;
            nextCursor = Vector2Int.zero;
            claimedCells = null;
            unusedWorldPoints = new List<Vector2Int>();
            return false;
        }

        // Start/End/막다른길 캡 전용 "터미널 연결" — 진입점 중 아무거나 하나만 커서에 맞으면
        // 성립한다(출구 소켓 요구 없음). 방식 1의 TryConnect/TryConnectBranch는 진입점이
        // 2개 이상이어야만 성립하도록 되어 있어 단일 진입점 청크(현재 모든 EndLine 프리팹이
        // 이 경우)를 절대 배치할 수 없는 문제가 있었다 — 그 형태를 그대로 포팅하지 않기 위해
        // 별도로 만든 함수.
        internal static bool TryConnectGridTerminal(GridChunkDatabaseEntry entry, Vector2Int cursor, HashSet<Vector2Int> occupied,
            System.Random rng, out Vector2Int origin, out List<Vector2Int> claimedCells)
        {
            List<Vector2Int> entrances = entry.GetAllEntrances();
            List<int> order = new List<int>();
            for (int i = 0; i < entrances.Count; i++) order.Add(i);
            Shuffle(order, rng);

            foreach (int inIdx in order)
            {
                Vector2Int inSocket = entrances[inIdx];
                Vector2Int candidateOrigin = cursor - inSocket;

                if (candidateOrigin.x % GridChunkData.CellSize != 0 || candidateOrigin.y % GridChunkData.CellSize != 0)
                    continue;

                List<Vector2Int> cells = OccupiedCellsFor(candidateOrigin, entry.footprint);
                if (CellsConflict(cells, occupied)) continue;

                origin = candidateOrigin;
                claimedCells = cells;
                return true;
            }

            origin = Vector2Int.zero;
            claimedCells = null;
            return false;
        }

        internal static bool IsDeadEndEntry(GridChunkDatabaseEntry entry)
        {
            List<Vector2Int> all = entry.GetAllEntrances();
            for (int i = 1; i < all.Count; i++)
                if (all[i] != all[0]) return false;
            return true; // 모든 진입점이 동일 좌표 -> 입구=출구인 막다른길
        }

        internal static GridChunkType RandomContentType(System.Random rng)
        {
            GridChunkType[] options = { GridChunkType.Combat, GridChunkType.Puzzle, GridChunkType.Treasure };
            return options[rng.Next(options.Length)];
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
