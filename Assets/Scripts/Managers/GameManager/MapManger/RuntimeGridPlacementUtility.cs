using System.Collections.Generic;
using UnityEngine;
using GridMapSystem;

internal static class RuntimeGridPlacementUtility
{
    internal const string RootName = "GeneratedGridMap";

    internal class GridPlacementFrame
    {
        public GridChunkType desiredType;
        public Vector2Int cursorBefore;
        public HashSet<GridChunkDatabaseEntry> excluded = new HashSet<GridChunkDatabaseEntry>();
        public GridChunkDatabaseEntry placedEntry;
        public GameObject instance;
        public Vector2Int cursorAfter;
        public List<Vector2Int> claimedCells;
        public int queuedSideBranchCount;
    }

    internal static List<Vector2Int> OccupiedCellsFor(Vector2Int origin, Vector2Int footprint)
    {
        int baseX = origin.x / GridChunkData.CellSize;
        int baseY = origin.y / GridChunkData.CellSize;
        var cells = new List<Vector2Int>();

        for (int x = 0; x < footprint.x; x++)
            for (int y = 0; y < footprint.y; y++)
                cells.Add(new Vector2Int(baseX + x, baseY + y));

        return cells;
    }

    internal static bool TryConnectBranchGrid(
        GridChunkDatabaseEntry entry,
        Vector2Int cursor,
        HashSet<Vector2Int> occupied,
        System.Random rng,
        out Vector2Int origin,
        out Vector2Int nextCursor,
        out List<Vector2Int> claimedCells,
        out List<Vector2Int> unusedWorldPoints)
    {
        List<Vector2Int> entrances = entry.GetAllEntrances();
        List<int> order = BuildShuffledIndexList(entrances.Count, rng);

        foreach (int inIdx in order)
        {
            Vector2Int inSocket = entrances[inIdx];
            Vector2Int candidateOrigin = cursor - inSocket;

            if (candidateOrigin.x % GridChunkData.CellSize != 0 || candidateOrigin.y % GridChunkData.CellSize != 0)
                continue;

            List<Vector2Int> cells = OccupiedCellsFor(candidateOrigin, entry.footprint);
            if (CellsConflict(cells, occupied)) continue;

            List<int> otherIdx = new List<int>();
            for (int i = 0; i < entrances.Count; i++)
                if (i != inIdx) otherIdx.Add(i);

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

    internal static bool TryConnectGridTerminal(
        GridChunkDatabaseEntry entry,
        Vector2Int cursor,
        HashSet<Vector2Int> occupied,
        System.Random rng,
        out Vector2Int origin,
        out List<Vector2Int> claimedCells)
    {
        List<Vector2Int> entrances = entry.GetAllEntrances();
        List<int> order = BuildShuffledIndexList(entrances.Count, rng);

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

        return true;
    }

    internal static bool IsThreeDirEntry(GridChunkDatabaseEntry entry)
    {
        return entry.GetAllEntrances().Count >= 3;
    }

    internal static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool CellsConflict(List<Vector2Int> cells, HashSet<Vector2Int> occupied)
    {
        foreach (var c in cells)
            if (occupied.Contains(c)) return true;

        return false;
    }

    private static List<int> BuildShuffledIndexList(int count, System.Random rng)
    {
        var order = new List<int>();
        for (int i = 0; i < count; i++)
            order.Add(i);

        Shuffle(order, rng);
        return order;
    }
}
