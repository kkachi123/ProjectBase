namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>좌우 이동과 하강으로 경로를 만들고 검증된 템플릿을 선택한다.</summary>
    public static class MapPlanner
    {
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
                RoomTemplateSnapshot chosen = candidates[random.Next(candidates.Count)];
                rooms.Add(new MapRoomPlan(cell, chosen.Id, chosen.Type, openings, entry, exit,
                    RoomTerrainPlanner.Select(seed, cell, chosen)));
                if (isStart) start = RoomDirection.Origin(cell) + chosen.PlayerStart;
                if (isGoal) goal = RoomDirection.Origin(cell) + chosen.Goal;
            }
            AddOptionalRoom(width, height, seed, path, indexes, templates, rooms);
            plan = new MapPlan(width, height, seed, rooms, path, start, goal);
            error = null;
            return true;
        }

        // 필수 경로를 먼저 확정하고, 검증된 왕복 연결이 있는 경우에만 막다른 방 하나를 추가한다.
        private static void AddOptionalRoom(int width, int height, int seed, List<Vector2Int> path,
            Dictionary<Vector2Int, int> pathIndexes, List<RoomTemplateSnapshot> templates, List<MapRoomPlan> rooms)
        {
            var choices = new List<(int mainIndex, int sideIndex, RoomOpening direction, RoomTemplateSnapshot main, RoomTemplateSnapshot side)>();
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2Int cell = path[i];
                MapRoomPlan main = rooms[cell.y * width + cell.x];
                foreach (RoomOpening direction in new[] { RoomOpening.East, RoomOpening.West })
                {
                    if ((main.Openings & direction) != 0) continue;
                    Vector2Int sideCell = cell + RoomDirection.Offset(direction);
                    if (sideCell.x < 0 || sideCell.x >= width || sideCell.y < 0 || sideCell.y >= height || pathIndexes.ContainsKey(sideCell)) continue;
                    RoomOpening sideEntry = RoomDirection.Opposite(direction);
                    foreach (RoomTemplateSnapshot candidate in templates)
                    {
                        if (candidate.Openings != (main.Openings | direction) ||
                            !candidate.Supports(main.Entry, main.Exit) || !candidate.Supports(main.Entry, direction) ||
                            !candidate.Supports(direction, main.Exit)) continue;
                        foreach (RoomTemplateSnapshot side in templates)
                            if (side.Openings == sideEntry && (side.VerifiedDeadEndReturns & sideEntry) != 0)
                                choices.Add((cell.y * width + cell.x, sideCell.y * width + sideCell.x, direction, candidate, side));
                    }
                }
            }
            if (choices.Count == 0) return;
            uint hash = unchecked(((uint)seed ^ 0x6A09E667u) * 1664525u + 1013904223u);
            var choice = choices[(int)(((ulong)hash * (uint)choices.Count) >> 32)];
            MapRoomPlan oldMain = rooms[choice.mainIndex], oldSide = rooms[choice.sideIndex];
            RoomOpening back = RoomDirection.Opposite(choice.direction);
            rooms[choice.mainIndex] = new MapRoomPlan(oldMain.Cell, choice.main.Id, choice.main.Type, choice.main.Openings,
                oldMain.Entry, oldMain.Exit, RoomTerrainPlanner.Select(seed, oldMain.Cell, choice.main), choice.direction);
            rooms[choice.sideIndex] = new MapRoomPlan(oldSide.Cell, choice.side.Id, choice.side.Type, choice.side.Openings,
                back, back, RoomTerrainPlanner.Select(seed, oldSide.Cell, choice.side), isOptional: true,
                content: RoomContentPlanner.Select(seed, oldSide.Cell, choice.side));
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
