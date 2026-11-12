namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>좌우 이동과 하강으로 경로를 만들고 검증된 템플릿을 선택한다.</summary>
    public static class MapPlanner
    {
        public const int MaxOptionalRooms = 3;
        public static MapPlanningResult Create(int width, int height, int seed, int maxAttempts,
            IReadOnlyList<RoomTemplateSnapshot> templates)
        {
            List<string> inputErrors = MapPlanValidator.ValidateInput(width, height, maxAttempts, templates);
            if (inputErrors.Count > 0) return MapPlanningResult.Failed($"seed={seed}: {string.Join("; ", inputErrors)}", 0);

            var ordered = new List<RoomTemplateSnapshot>(templates);
            ordered.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var random = new StableRandom(seed);
            string reason = "경로를 만들지 못했습니다.";
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                List<Vector2Int> path = CreatePath(width, height, random);
                if (!TrySelect(width, height, seed, path, ordered, random, out MapPlan plan, out reason))
                    continue;
                List<string> errors = MapPlanValidator.Validate(plan, ordered);
                if (errors.Count == 0) return MapPlanningResult.Succeeded(plan, attempt);
                reason = string.Join("; ", errors);
            }
            return MapPlanningResult.Failed($"seed={seed}, attempts={maxAttempts}: {reason}", maxAttempts);
        }

        private static List<Vector2Int> CreatePath(int width, int height, StableRandom random)
        {
            var path = new List<Vector2Int>();
            int x = random.Next(width);
            for (int y = 0; y < height; y++)
            {
                path.Add(new Vector2Int(x, y));
                int destination = random.Next(width);
                while (x != destination)
                {
                    x += x < destination ? 1 : -1;
                    path.Add(new Vector2Int(x, y));
                }
            }
            return path;
        }

        private static bool TrySelect(int width, int height, int seed, List<Vector2Int> path,
            List<RoomTemplateSnapshot> templates, StableRandom random, out MapPlan plan, out string error)
        {
            var indexes = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < path.Count; i++) indexes.Add(path[i], i);
            var rooms = new List<MapRoomPlan>(width * height);
            var variantRandom = new StableRandom(MixSeed(seed, 0x91E10DA5u));
            Vector3 start = default, goal = default;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var cell = new Vector2Int(x, y);
                bool onPath = indexes.TryGetValue(cell, out int index);
                RoomOpening entry = onPath && index > 0 ? RoomDirection.Between(cell, path[index - 1]) : RoomOpening.None;
                RoomOpening exit = onPath && index < path.Count - 1 ? RoomDirection.Between(cell, path[index + 1]) : RoomOpening.None;
                RoomOpening openings = entry | exit;
                bool isStart = onPath && index == 0;
                bool isGoal = onPath && index == path.Count - 1;
                var candidates = new List<RoomTemplateSnapshot>();
                foreach (RoomTemplateSnapshot template in templates)
                {
                    if (template.Openings != openings) continue;
                    if (!onPath && template.Type != RoomType.Side) continue;
                    if (isStart && (!template.HasPlayerStart || (template.VerifiedStartExits & exit) == 0)) continue;
                    if (isGoal && (!template.HasGoal || (template.VerifiedGoalEntries & entry) == 0)) continue;
                    if (onPath && !isStart && !isGoal && !template.Supports(entry, exit)) continue;
                    candidates.Add(template);
                }
                if (candidates.Count == 0)
                {
                    plan = null;
                    error = $"후보 없음: cell={cell}, openings={openings}, entry={entry}, exit={exit}, start={isStart}, goal={isGoal}";
                    return false;
                }
                RoomTemplateSnapshot chosen = ChooseGrouped(candidates, random, variantRandom);
                rooms.Add(new MapRoomPlan(cell, chosen.Id, chosen.Type, openings, entry, exit,
                    RoomTerrainPlanner.Select(seed, cell, chosen)));
                if (isStart) start = RoomDirection.Origin(cell) + chosen.PlayerStart;
                if (isGoal) goal = RoomDirection.Origin(cell) + chosen.Goal;
            }
            AddOptionalRooms(width, height, seed, path, indexes, templates, rooms);
            var byId = new Dictionary<string, RoomTemplateSnapshot>(StringComparer.Ordinal);
            foreach (var template in templates) byId.Add(template.Id, template);
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                bool mainPath = indexes.TryGetValue(room.Cell, out int pathIndex) && pathIndex > 0 && pathIndex < path.Count - 1;
                if (!room.IsOptional && !mainPath) continue;
                var role = room.IsOptional ? ContentRoomRole.Optional : ContentRoomRole.MainPath;
                rooms[i] = new MapRoomPlan(room.Cell, room.TemplateId, room.Type, room.Openings, room.Entry, room.Exit,
                    room.Terrain, room.Branch, room.IsOptional, RoomContentPlanner.Select(seed, room.Cell, byId[room.TemplateId], role),
                    RoomHazardPlanner.Select(seed, room.Cell, byId[room.TemplateId]));
            }
            plan = new MapPlan(width, height, seed, rooms, path, start, goal);
            error = null;
            return true;
        }

        // 필수 경로에 검증된 막다른 방을 연결한다.
        private static void AddOptionalRooms(int width, int height, int seed, List<Vector2Int> path,
            Dictionary<Vector2Int, int> pathIndexes, List<RoomTemplateSnapshot> templates, List<MapRoomPlan> rooms)
        {
            var branchRandom = new StableRandom(unchecked(seed ^ (int)0x6A09E667u));
            var branchVariantRandom = new StableRandom(MixSeed(seed, 0xBB67AE85u));
            int added = 0;
            while (added < MaxOptionalRooms)
            {
                var pairs = new List<(int index, RoomOpening directions)>();
                var singles = new List<(int index, RoomOpening directions)>();
                for (int i = 1; i < path.Count - 1; i++)
                {
                    Vector2Int cell = path[i];
                    int index = cell.y * width + cell.x;
                    MapRoomPlan main = rooms[index];
                    if (main.Branch != RoomOpening.None) continue;
                    foreach (RoomOpening directions in new[] { RoomOpening.East | RoomOpening.West, RoomOpening.East, RoomOpening.West })
                    {
                        bool pair = directions == (RoomOpening.East | RoomOpening.West);
                        if ((directions & main.Openings) != 0 || (pair && added + 2 > MaxOptionalRooms)) continue;
                        bool valid = true;
                        foreach (RoomOpening direction in new[] { RoomOpening.East, RoomOpening.West })
                        {
                            if ((directions & direction) == 0) continue;
                            Vector2Int sideCell = cell + RoomDirection.Offset(direction);
                            if (sideCell.x < 0 || sideCell.x >= width || sideCell.y < 0 || sideCell.y >= height ||
                                pathIndexes.ContainsKey(sideCell) || rooms[sideCell.y * width + sideCell.x].IsOptional ||
                                SideCandidates(templates, RoomDirection.Opposite(direction)).Count == 0)
                            { valid = false; break; }
                        }
                        if (!valid || MainCandidates(templates, main, directions).Count == 0) continue;
                        (pair ? pairs : singles).Add((index, directions));
                    }
                }
                var choices = pairs.Count > 0 ? pairs : singles;
                if (choices.Count == 0) break;
                var choice = choices[branchRandom.Next(choices.Count)];
                MapRoomPlan oldMain = rooms[choice.index];
                RoomTemplateSnapshot chosenMain = ChooseGrouped(MainCandidates(templates, oldMain, choice.directions), branchRandom, branchVariantRandom);
                rooms[choice.index] = new MapRoomPlan(oldMain.Cell, chosenMain.Id, chosenMain.Type, chosenMain.Openings,
                    oldMain.Entry, oldMain.Exit, RoomTerrainPlanner.Select(seed, oldMain.Cell, chosenMain), choice.directions);
                foreach (RoomOpening direction in new[] { RoomOpening.East, RoomOpening.West })
                {
                    if ((choice.directions & direction) == 0) continue;
                    Vector2Int sideCell = oldMain.Cell + RoomDirection.Offset(direction);
                    int sideIndex = sideCell.y * width + sideCell.x;
                    RoomOpening back = RoomDirection.Opposite(direction);
                    RoomTemplateSnapshot chosenSide = ChooseGrouped(SideCandidates(templates, back), branchRandom, branchVariantRandom);
                    rooms[sideIndex] = new MapRoomPlan(sideCell, chosenSide.Id, chosenSide.Type, chosenSide.Openings,
                        back, back, RoomTerrainPlanner.Select(seed, sideCell, chosenSide), isOptional: true);
                    added++;
                }
            }
        }

        private static List<RoomTemplateSnapshot> MainCandidates(List<RoomTemplateSnapshot> templates, MapRoomPlan main, RoomOpening directions)
        {
            var result = new List<RoomTemplateSnapshot>();
            foreach (var candidate in templates)
                if (candidate.Openings == (main.Entry | main.Exit | directions) && SupportsBranches(candidate, main.Entry, main.Exit, directions))
                    result.Add(candidate);
            return result;
        }

        private static List<RoomTemplateSnapshot> SideCandidates(List<RoomTemplateSnapshot> templates, RoomOpening entry)
        {
            var result = new List<RoomTemplateSnapshot>();
            foreach (var side in templates)
                if (side.Openings == entry && (side.VerifiedDeadEndReturns & entry) != 0) result.Add(side);
            return result;
        }

        internal static bool SupportsBranches(RoomTemplateSnapshot template, RoomOpening entry, RoomOpening exit, RoomOpening branches)
        {
            if (!template.Supports(entry, exit)) return false;
            foreach (RoomOpening direction in new[] { RoomOpening.East, RoomOpening.West })
                if ((branches & direction) != 0 && (!template.Supports(entry, direction) || !template.Supports(direction, exit))) return false;
            return branches != (RoomOpening.East | RoomOpening.West) ||
                (template.Supports(RoomOpening.East, RoomOpening.West) && template.Supports(RoomOpening.West, RoomOpening.East));
        }

        private static RoomTemplateSnapshot ChooseGrouped(List<RoomTemplateSnapshot> candidates,
            StableRandom groupRandom, StableRandom variantRandom)
        {
            var groups = new SortedDictionary<string, List<RoomTemplateSnapshot>>(StringComparer.Ordinal);
            foreach (RoomTemplateSnapshot candidate in candidates)
            {
                if (!groups.TryGetValue(candidate.BaseGroupId, out var variants))
                    groups.Add(candidate.BaseGroupId, variants = new List<RoomTemplateSnapshot>());
                variants.Add(candidate);
            }
            int groupIndex = groupRandom.Next(groups.Count);
            foreach (var group in groups.Values)
                if (groupIndex-- == 0)
                    return group.Count == 1 ? group[0] : group[variantRandom.Next(group.Count)];
            throw new InvalidOperationException("Base 그룹을 선택하지 못했습니다.");
        }

        private static int MixSeed(int seed, uint salt)
        {
            uint value = unchecked((uint)seed ^ salt);
            value ^= value >> 16;
            value = unchecked(value * 0x7FEB352Du);
            value ^= value >> 15;
            value = unchecked(value * 0x846CA68Bu);
            value ^= value >> 16;
            return unchecked((int)value);
        }

        // System.Random 구현 변경에 영향을 받지 않도록 알고리즘을 고정한다.
        private sealed class StableRandom
        {
            private uint _state;
            public StableRandom(int seed) => _state = unchecked((uint)seed) ^ 0xA3C59AC3u;
            public int Next(int exclusiveMax)
            {
                _state = unchecked(_state * 1664525u + 1013904223u);
                return (int)(((ulong)_state * (uint)exclusiveMax) >> 32);
            }
        }
    }
}
