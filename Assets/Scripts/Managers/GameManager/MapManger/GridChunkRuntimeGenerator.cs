using System.Collections.Generic;
using UnityEngine;
using GridMapSystem;
using static RuntimeGridPlacementUtility;

public static class GridChunkRuntimeGenerator
{
    private class SideBranchTask
    {
        public Vector2Int cursor;
        public int stepBudget;
    }

    public static GeneratedMapInfo Generate(GridMapGenerationSettings settings, GridChunkGenerationContext context = null)
    {
        if (settings == null)
        {
            Debug.LogError("[GridChunkRuntimeGenerator] GridMapGenerationSettings is missing.");
            return null;
        }

        return GenerateGrid(
            settings.database,
            settings.contentChunkCount,
            settings.ResolveSeed(),
            settings.spawnPrefabs,
            settings.maxThreeDirCount,
            context);
    }

    public static GeneratedMapInfo GenerateGrid(
        GridChunkDatabase db,
        int contentChunkCount,
        int seed,
        GridSpawnPrefabSet spawnPrefabs = null,
        int maxThreeDirCount = 2,
        GridChunkGenerationContext context = null)
    {
        spawnPrefabs = spawnPrefabs ?? new GridSpawnPrefabSet();
        context = context ?? new GridChunkGenerationContext();

        if (db == null)
        {
            Debug.LogError("[GridChunkRuntimeGenerator] GridChunkDatabase is missing.");
            return null;
        }

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) context.Destroy(oldRoot);

        GameObject root = new GameObject(RootName);
        Vector3 playerSpawnPosition = Vector3.zero;
        Vector3 endPosition = Vector3.zero;

        System.Random rng = new System.Random(seed);
        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        List<GridPlacementFrame> stack = new List<GridPlacementFrame>();
        List<SideBranchTask> sideBranches = new List<SideBranchTask>();

        const int maxBacktracks = 3000;
        int totalBacktracks = 0;
        int step = 0;
        int contentPlaced = 0;
        int globalIndex = 0;
        int threeDirPlaced = 0;

        Vector2Int cursor = Vector2Int.zero;

        List<GridChunkDatabaseEntry> startCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
        startCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.Start);
        Shuffle(startCandidates, rng);

        bool placedStart = false;
        foreach (GridChunkDatabaseEntry entry in startCandidates)
        {
            List<Vector2Int> entrances = entry.GetAllEntrances();
            if (entrances.Count == 0) continue;

            Vector2Int origin = Vector2Int.zero;
            Vector2Int chosenEntrance = entrances[0];
            List<Vector2Int> claimedCells = OccupiedCellsFor(origin, entry.footprint);

            GameObject inst = context.Instantiate(entry.prefab, root.transform);
            if (inst == null) continue;

            inst.transform.position = new Vector3(origin.x, origin.y, 0f);
            inst.name = entry.prefabName + "_start" + globalIndex++;

            foreach (var c in claimedCells)
                occupiedCells.Add(c);

            TryGetSpawnWorldPosition(inst, out playerSpawnPosition);
            cursor = origin + chosenEntrance;
            placedStart = true;
            break;
        }

        if (!placedStart)
        {
            Debug.LogError("[GridChunkRuntimeGenerator] Start EndLine chunk could not be placed.");
            context.Destroy(root);
            return null;
        }

        List<GridChunkDatabaseEntry> endCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
        endCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.End);
        System.Func<Vector2Int, bool> canPlaceEndAt = (c) =>
        {
            foreach (GridChunkDatabaseEntry entry in endCandidates)
                if (TryConnectGridTerminal(entry, c, occupiedCells, rng, out Vector2Int _origin, out List<Vector2Int> _cells))
                    return true;

            return false;
        };

        while (true)
        {
            Vector2Int cursorSoFar = step > 0 ? stack[step - 1].cursorAfter : cursor;

            if (contentPlaced >= contentChunkCount)
            {
                if (canPlaceEndAt(cursorSoFar)) break;

                if (step == 0)
                {
                    Debug.LogWarning("[GridChunkRuntimeGenerator] Could not find an end connection for an empty content route.");
                    break;
                }

                totalBacktracks++;
                if (totalBacktracks > maxBacktracks)
                {
                    Debug.LogWarning($"[GridChunkRuntimeGenerator] End connection search exceeded {maxBacktracks} backtracks.");
                    break;
                }

                step--;
                GridPlacementFrame endRetryFrame = stack[step];
                RollbackFrame(endRetryFrame, occupiedCells, sideBranches, ref threeDirPlaced, ref contentPlaced, true, context);
                if (stack.Count > step + 1)
                    stack.RemoveRange(step + 1, stack.Count - step - 1);

                continue;
            }

            if (step == stack.Count)
            {
                stack.Add(new GridPlacementFrame { desiredType = GridChunkType.Content, cursorBefore = cursorSoFar });
            }

            GridPlacementFrame frame = stack[step];
            List<GridChunkDatabaseEntry> candidates = new List<GridChunkDatabaseEntry>(db.GetByType(frame.desiredType));
            candidates.RemoveAll(e => frame.excluded.Contains(e) || IsDeadEndEntry(e));
            if (threeDirPlaced >= maxThreeDirCount)
                candidates.RemoveAll(IsThreeDirEntry);

            Shuffle(candidates, rng);

            bool placed = false;
            foreach (GridChunkDatabaseEntry entry in candidates)
            {
                if (!TryConnectBranchGrid(
                        entry,
                        frame.cursorBefore,
                        occupiedCells,
                        rng,
                        out Vector2Int origin,
                        out Vector2Int nextCursor,
                        out List<Vector2Int> claimedCells,
                        out List<Vector2Int> unusedWorldPoints))
                    continue;

                GameObject inst = context.Instantiate(entry.prefab, root.transform);
                if (inst == null) continue;

                inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                inst.name = entry.prefabName + "_main" + globalIndex++;
                SpawnContentFor(inst, rng, spawnPrefabs, context);

                foreach (var c in claimedCells)
                    occupiedCells.Add(c);

                contentPlaced++;
                if (IsThreeDirEntry(entry))
                    threeDirPlaced++;

                frame.placedEntry = entry;
                frame.instance = inst;
                frame.cursorAfter = nextCursor;
                frame.claimedCells = claimedCells;
                frame.queuedSideBranchCount = unusedWorldPoints.Count;

                foreach (Vector2Int p in unusedWorldPoints)
                    sideBranches.Add(new SideBranchTask { cursor = p, stepBudget = rng.Next(2, 6) });

                step++;
                placed = true;
                break;
            }

            if (!placed)
            {
                if (step == 0)
                {
                    Debug.LogError("[GridChunkRuntimeGenerator] No content chunk could be placed at the first step.");
                    break;
                }

                totalBacktracks++;
                if (totalBacktracks > maxBacktracks)
                {
                    Debug.LogWarning($"[GridChunkRuntimeGenerator] Backtracking exceeded {maxBacktracks}; generated {contentPlaced} content chunks.");
                    break;
                }

                step--;
                GridPlacementFrame prevFrame = stack[step];
                RollbackFrame(prevFrame, occupiedCells, sideBranches, ref threeDirPlaced, ref contentPlaced, false, context);
                if (stack.Count > step + 1)
                    stack.RemoveRange(step + 1, stack.Count - step - 1);
            }
        }

        Vector2Int mainEndCursor = step > 0 ? stack[step - 1].cursorAfter : cursor;
        int branchGuard = GenerateSideBranches(db, rng, spawnPrefabs, context, root.transform, occupiedCells, sideBranches, ref threeDirPlaced, maxThreeDirCount, ref globalIndex);
        bool placedEnd = PlaceEndChunk(endCandidates, rng, context, root.transform, occupiedCells, mainEndCursor, ref globalIndex, out endPosition);

        if (!placedEnd)
            Debug.LogWarning("[GridChunkRuntimeGenerator] End chunk could not be placed.");

        GeneratedMapInfo info = root.AddComponent<GeneratedMapInfo>();
        info.playerSpawnPosition = playerSpawnPosition;
        info.endPosition = endPosition;

        Debug.Log($"[GridChunkRuntimeGenerator] Grid generated: Content {contentPlaced}/{contentChunkCount}, 3Direction {threeDirPlaced}/{maxThreeDirCount}, side branches {branchGuard}, seed={seed}");
        return info;
    }

    public static void ClearGeneratedMap(GridChunkGenerationContext context = null)
    {
        context = context ?? new GridChunkGenerationContext();
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
            context.Destroy(oldRoot);
    }

    private static void SpawnContentFor(GameObject chunkInstance, System.Random rng, GridSpawnPrefabSet spawnPrefabs, GridChunkGenerationContext context)
    {
        GridChunkData data = chunkInstance.GetComponent<GridChunkData>();
        if (data == null) return;

        int index = 0;
        foreach (var spawn in data.GetResolvedSpawns(rng))
        {
            GameObject prefab = spawnPrefabs.Get(spawn.kind);
            if (prefab == null) continue;

            GameObject spawned = context.Instantiate(prefab, chunkInstance.transform);
            if (spawned == null) continue;

            spawned.transform.position = chunkInstance.transform.position + new Vector3(spawn.position.x, spawn.position.y, 0f);
            spawned.name = $"{spawn.kind}_{index++}";
        }
    }

    private static bool TryGetSpawnWorldPosition(GameObject chunkInstance, out Vector3 worldPosition)
    {
        GridChunkData data = chunkInstance.GetComponent<GridChunkData>();
        if (data == null)
        {
            worldPosition = default;
            return false;
        }

        List<Vector2Int> points = data.GetSpawnPoints();
        if (points.Count == 0)
        {
            worldPosition = default;
            return false;
        }

        worldPosition = chunkInstance.transform.position + new Vector3(points[0].x, points[0].y, 0f);
        return true;
    }

    private static void RollbackFrame(
        GridPlacementFrame frame,
        HashSet<Vector2Int> occupiedCells,
        List<SideBranchTask> sideBranches,
        ref int threeDirPlaced,
        ref int contentPlaced,
        bool alwaysRemoveContent,
        GridChunkGenerationContext context)
    {
        if (frame.instance != null)
            context.Destroy(frame.instance);

        if (frame.claimedCells != null)
        {
            foreach (var c in frame.claimedCells)
                occupiedCells.Remove(c);
        }

        if (frame.queuedSideBranchCount > 0)
            sideBranches.RemoveRange(sideBranches.Count - frame.queuedSideBranchCount, frame.queuedSideBranchCount);

        if (frame.placedEntry != null && IsThreeDirEntry(frame.placedEntry))
            threeDirPlaced--;

        if ((alwaysRemoveContent || frame.desiredType != GridChunkType.Transition) && frame.placedEntry != null)
            contentPlaced--;

        frame.excluded.Add(frame.placedEntry);
        frame.placedEntry = null;
        frame.instance = null;
        frame.claimedCells = null;
        frame.queuedSideBranchCount = 0;
    }

    private static int GenerateSideBranches(
        GridChunkDatabase db,
        System.Random rng,
        GridSpawnPrefabSet spawnPrefabs,
        GridChunkGenerationContext context,
        Transform root,
        HashSet<Vector2Int> occupiedCells,
        List<SideBranchTask> sideBranches,
        ref int threeDirPlaced,
        int maxThreeDirCount,
        ref int globalIndex)
    {
        int branchGuard = 0;
        while (sideBranches.Count > 0 && branchGuard < 500)
        {
            branchGuard++;
            SideBranchTask task = sideBranches[0];
            sideBranches.RemoveAt(0);

            Vector2Int branchCursor = task.cursor;
            int placedInBranch = 0;
            int branchFailSafe = 0;

            while (placedInBranch < task.stepBudget && branchFailSafe < 20)
            {
                branchFailSafe++;
                List<GridChunkDatabaseEntry> candidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.Content));
                candidates.RemoveAll(e => IsDeadEndEntry(e));
                if (threeDirPlaced >= maxThreeDirCount)
                    candidates.RemoveAll(IsThreeDirEntry);

                Shuffle(candidates, rng);

                bool placedHere = false;
                foreach (GridChunkDatabaseEntry entry in candidates)
                {
                    if (!TryConnectBranchGrid(
                            entry,
                            branchCursor,
                            occupiedCells,
                            rng,
                            out Vector2Int origin,
                            out Vector2Int nextCursor,
                            out List<Vector2Int> claimedCells,
                            out List<Vector2Int> unusedWorldPoints))
                        continue;

                    GameObject inst = context.Instantiate(entry.prefab, root);
                    if (inst == null) continue;

                    inst.transform.position = new Vector3(origin.x, origin.y, 0f);
                    inst.name = entry.prefabName + "_side" + globalIndex++;
                    SpawnContentFor(inst, rng, spawnPrefabs, context);

                    foreach (var c in claimedCells)
                        occupiedCells.Add(c);

                    placedInBranch++;
                    if (IsThreeDirEntry(entry))
                        threeDirPlaced++;

                    foreach (Vector2Int p in unusedWorldPoints)
                        sideBranches.Add(new SideBranchTask { cursor = p, stepBudget = rng.Next(2, 6) });

                    branchCursor = nextCursor;
                    placedHere = true;
                    break;
                }

                if (!placedHere) break;
            }

            PlaceDeadEndCap(db, rng, spawnPrefabs, context, root, occupiedCells, branchCursor, ref globalIndex);
        }

        return branchGuard;
    }

    private static void PlaceDeadEndCap(
        GridChunkDatabase db,
        System.Random rng,
        GridSpawnPrefabSet spawnPrefabs,
        GridChunkGenerationContext context,
        Transform root,
        HashSet<Vector2Int> occupiedCells,
        Vector2Int branchCursor,
        ref int globalIndex)
    {
        List<GridChunkDatabaseEntry> deadEndCandidates = new List<GridChunkDatabaseEntry>(db.GetByType(GridChunkType.EndLine));
        deadEndCandidates.RemoveAll(e => e.endLineRole != GridEndLineRole.Normal);
        Shuffle(deadEndCandidates, rng);

        foreach (GridChunkDatabaseEntry entry in deadEndCandidates)
        {
            if (!TryConnectGridTerminal(entry, branchCursor, occupiedCells, rng, out Vector2Int origin, out List<Vector2Int> claimedCells))
                continue;

            GameObject inst = context.Instantiate(entry.prefab, root);
            if (inst == null) continue;

            inst.transform.position = new Vector3(origin.x, origin.y, 0f);
            inst.name = entry.prefabName + "_cap" + globalIndex++;
            SpawnContentFor(inst, rng, spawnPrefabs, context);

            foreach (var c in claimedCells)
                occupiedCells.Add(c);

            break;
        }
    }

    private static bool PlaceEndChunk(
        List<GridChunkDatabaseEntry> endCandidates,
        System.Random rng,
        GridChunkGenerationContext context,
        Transform root,
        HashSet<Vector2Int> occupiedCells,
        Vector2Int mainEndCursor,
        ref int globalIndex,
        out Vector3 endPosition)
    {
        endPosition = Vector3.zero;
        Shuffle(endCandidates, rng);

        foreach (GridChunkDatabaseEntry entry in endCandidates)
        {
            if (!TryConnectGridTerminal(entry, mainEndCursor, occupiedCells, rng, out Vector2Int origin, out List<Vector2Int> claimedCells))
                continue;

            GameObject inst = context.Instantiate(entry.prefab, root);
            if (inst == null) continue;

            inst.transform.position = new Vector3(origin.x, origin.y, 0f);
            inst.name = entry.prefabName + "_end" + globalIndex++;
            TryGetSpawnWorldPosition(inst, out endPosition);

            foreach (var c in claimedCells)
                occupiedCells.Add(c);

            return true;
        }

        return false;
    }
}
