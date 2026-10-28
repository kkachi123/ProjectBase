namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>완성된 계획과 입력 스냅샷의 일관성을 검사한다.</summary>
    public static class MapPlanValidator
    {
        internal const RoomOpening AllOpenings = RoomOpening.North | RoomOpening.East | RoomOpening.South | RoomOpening.West;

        public static List<string> ValidateInput(int width, int height, int maxAttempts,
            IReadOnlyList<RoomTemplateSnapshot> templates)
        {
            var errors = new List<string>();
            if (width < 2 || height < 2 || width > 16 || height > 16)
                errors.Add("격자 크기는 각 축 2~16이어야 합니다.");
            if (maxAttempts < 1 || maxAttempts > 10000)
                errors.Add("시도 횟수는 1~10000이어야 합니다.");
            if (templates == null || templates.Count == 0)
            {
                errors.Add("템플릿 목록이 비어 있습니다.");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoomTemplateSnapshot template in templates)
            {
                if (template == null) { errors.Add("템플릿이 null입니다."); continue; }
                if (string.IsNullOrWhiteSpace(template.Id) || !ids.Add(template.Id))
                    errors.Add($"비어 있거나 중복된 템플릿 ID: {template.Id}");
                if (!Enum.IsDefined(typeof(RoomType), template.Type) || (template.Openings & ~AllOpenings) != 0 ||
                    !RoomDirection.MatchesType(template.Type, template.Openings))
                    errors.Add($"잘못된 방 유형 또는 출입구: {template.Id}");
                var passages = new HashSet<(RoomOpening, RoomOpening)>();
                foreach (RoomPassage passage in template.Passages)
                {
                    if (RoomDirection.Opposite(passage.entry) == RoomOpening.None ||
                        RoomDirection.Opposite(passage.exit) == RoomOpening.None || passage.entry == passage.exit ||
                        (template.Openings & passage.entry) == 0 || (template.Openings & passage.exit) == 0 ||
                        !passages.Add((passage.entry, passage.exit)))
                        errors.Add($"잘못되거나 중복된 통과 방향: {template.Id}");
                }
                if ((template.HasPlayerStart && !Inside(template.PlayerStart)) ||
                    (template.HasGoal && !Inside(template.Goal)))
                    errors.Add($"시작 또는 Goal 마커가 방 밖입니다: {template.Id}");
                if ((template.VerifiedStartExits & ~template.Openings) != 0 ||
                    (template.VerifiedGoalEntries & ~template.Openings) != 0 ||
                    (!template.HasPlayerStart && template.VerifiedStartExits != RoomOpening.None) ||
                    (!template.HasGoal && template.VerifiedGoalEntries != RoomOpening.None))
                    errors.Add($"시작 또는 Goal의 검증 방향이 잘못됐습니다: {template.Id}");
                if (template.VerifiedDeadEndReturns != RoomOpening.None &&
                    (template.VerifiedDeadEndReturns != template.Openings || RoomDirection.Opposite(template.Openings) == RoomOpening.None))
                    errors.Add($"막다른 방의 왕복 검증 방향이 잘못됐습니다: {template.Id}");
                var regions = new HashSet<int>();
                foreach (RoomTerrainSnapshot region in template.Terrain)
                {
                    if (region == null || region.RegionIndex < 0 || !regions.Add(region.RegionIndex))
                    { errors.Add($"잘못된 변형 영역: {template.Id}"); continue; }
                    if (!region.AllowEmptyPattern && region.PatternIds.Count == 0 || region.Cells.Count != region.Chances.Count)
                        errors.Add($"변형 선택지가 잘못됐습니다: {template.Id}");
                    var patternIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string id in region.PatternIds)
                        if (string.IsNullOrWhiteSpace(id) || !patternIds.Add(id)) errors.Add($"잘못된 패턴 ID: {template.Id}");
                    var positions = new HashSet<Vector2Int>();
                    for (int i = 0; i < region.Cells.Count; i++)
                        if (!positions.Add(region.Cells[i]) || i >= region.Chances.Count || region.Chances[i] < 0 || region.Chances[i] > 100)
                            errors.Add($"잘못된 확률 셀: {template.Id}");
                }
                var slots = new HashSet<int>();
                foreach (ContentSlotSnapshot slot in template.Content)
                {
                    if (slot == null || slot.SlotIndex < 0 || !slots.Add(slot.SlotIndex) || slot.EmptyWeight < 0)
                    { errors.Add($"잘못된 콘텐츠 슬롯: {template.Id}"); continue; }
                    long total = slot.EmptyWeight;
                    var options = new HashSet<string>(StringComparer.Ordinal);
                    foreach (ContentOptionSnapshot option in slot.Options)
                    {
                        if (option == null || string.IsNullOrWhiteSpace(option.Id) || !options.Add(option.Id) || option.Weight <= 0)
                        { errors.Add($"잘못된 콘텐츠 선택지: {template.Id}"); continue; }
                        total += option.Weight;
                    }
                    if (total <= 0 || total > 1000000) errors.Add($"잘못된 콘텐츠 가중치: {template.Id}");
                }
            }
            return errors;
        }

        public static List<string> Validate(MapPlan plan, IReadOnlyList<RoomTemplateSnapshot> templates)
        {
            var errors = new List<string>();
            if (plan == null) { errors.Add("계획이 없습니다."); return errors; }
            errors.AddRange(ValidateInput(plan.Width, plan.Height, 1, templates));
            if (errors.Count != 0) return errors;

            var byId = new Dictionary<string, RoomTemplateSnapshot>(StringComparer.Ordinal);
            foreach (var template in templates) byId.Add(template.Id, template);
            var rooms = new Dictionary<Vector2Int, MapRoomPlan>();
            int optionalCount = 0;
            foreach (MapRoomPlan room in plan.Rooms)
            {
                if (room == null || !InBounds(room.Cell, plan.Width, plan.Height) || rooms.ContainsKey(room.Cell))
                { errors.Add("방 좌표가 중복되거나 격자 밖입니다."); continue; }
                rooms.Add(room.Cell, room);
                if (room.IsOptional) optionalCount++;
                if (!byId.TryGetValue(room.TemplateId ?? "", out var template) ||
                    template.Type != room.Type || template.Openings != room.Openings)
                    errors.Add($"방의 템플릿 또는 출입구가 일치하지 않습니다: {room.Cell}");
                else
                {
                    var expected = RoomTerrainPlanner.Select(plan.Seed, room.Cell, template);
                    if (room.Terrain.Count != expected.Count) errors.Add($"지형 선택 개수가 다릅니다: {room.Cell}");
                    else for (int i = 0; i < expected.Count; i++)
                    {
                        RoomTerrainSelection actual = room.Terrain[i], wanted = expected[i];
                        if (actual == null || actual.RegionIndex != wanted.RegionIndex || actual.PatternId != wanted.PatternId ||
                            actual.FilledCells.Count != wanted.FilledCells.Count)
                        { errors.Add($"지형 선택이 일치하지 않습니다: {room.Cell}"); continue; }
                        for (int j = 0; j < actual.FilledCells.Count; j++)
                            if (actual.FilledCells[j] != wanted.FilledCells[j]) errors.Add($"확률 셀이 일치하지 않습니다: {room.Cell}");
                    }
                    var expectedContent = room.IsOptional ? RoomContentPlanner.Select(plan.Seed, room.Cell, template) : Array.Empty<ContentSelection>();
                    if (room.Content.Count != expectedContent.Count) errors.Add($"콘텐츠 선택 개수가 다릅니다: {room.Cell}");
                    else for (int i = 0; i < expectedContent.Count; i++)
                        if (room.Content[i] == null || room.Content[i].SlotIndex != expectedContent[i].SlotIndex ||
                            room.Content[i].OptionId != expectedContent[i].OptionId)
                            errors.Add($"콘텐츠 선택이 일치하지 않습니다: {room.Cell}");
                }
            }
            if (optionalCount > 1) errors.Add("선택 방은 최대 하나여야 합니다.");
            if (rooms.Count != plan.Width * plan.Height) errors.Add("격자의 방 개수가 일치하지 않습니다.");
            if (plan.Path.Count < 2) { errors.Add("주 경로가 너무 짧습니다."); return errors; }
            if (plan.Path[0].y != 0 || plan.Path[plan.Path.Count - 1].y != plan.Height - 1)
                errors.Add("시작과 Goal은 각각 첫 행과 마지막 행에 있어야 합니다.");

            var seen = new HashSet<Vector2Int>();
            for (int i = 0; i < plan.Path.Count; i++)
            {
                Vector2Int cell = plan.Path[i];
                if (!seen.Add(cell) || !rooms.TryGetValue(cell, out MapRoomPlan room))
                { errors.Add($"경로 방이 중복되거나 없습니다: {cell}"); continue; }
                RoomOpening entry = i == 0 ? RoomOpening.None : RoomDirection.Between(cell, plan.Path[i - 1]);
                RoomOpening exit = i == plan.Path.Count - 1 ? RoomOpening.None : RoomDirection.Between(cell, plan.Path[i + 1]);
                if ((i > 0 && entry == RoomOpening.None) || (i < plan.Path.Count - 1 && exit == RoomOpening.None) ||
                    exit == RoomOpening.North || entry == RoomOpening.South || room.Entry != entry || room.Exit != exit ||
                    room.IsOptional || room.Openings != (entry | exit | room.Branch) ||
                    (room.Branch != RoomOpening.None && (i == 0 || i == plan.Path.Count - 1 ||
                     (room.Branch != RoomOpening.East && room.Branch != RoomOpening.West) || (room.Branch & (entry | exit)) != 0)))
                    errors.Add($"주 경로의 방향이나 출입구가 잘못됐습니다: {cell}");
                if (!byId.TryGetValue(room.TemplateId ?? "", out var template)) continue;
                if (i == 0 && (!template.HasPlayerStart || (template.VerifiedStartExits & exit) == 0 || plan.PlayerStart != RoomDirection.Origin(cell) + template.PlayerStart))
                    errors.Add("시작 마커가 없거나 위치가 다릅니다.");
                if (i == plan.Path.Count - 1 && (!template.HasGoal || (template.VerifiedGoalEntries & entry) == 0 || plan.Goal != RoomDirection.Origin(cell) + template.Goal))
                    errors.Add("Goal 마커가 없거나 위치가 다릅니다.");
                if (i > 0 && i < plan.Path.Count - 1 && !template.Supports(entry, exit))
                    errors.Add($"검증된 통과 방향이 없습니다: {cell} {entry} → {exit}");
                if (room.Branch != RoomOpening.None && (!template.Supports(entry, room.Branch) || !template.Supports(room.Branch, exit)))
                    errors.Add($"검증된 선택 방 연결이 없습니다: {cell}");
            }
            foreach (MapRoomPlan room in rooms.Values)
            {
                if (!seen.Contains(room.Cell))
                {
                    if (room.IsOptional)
                    {
                        if (room.Branch != RoomOpening.None || RoomDirection.Opposite(room.Entry) == RoomOpening.None ||
                            room.Exit != room.Entry || room.Openings != room.Entry ||
                            !byId.TryGetValue(room.TemplateId ?? "", out var sideTemplate) ||
                            (sideTemplate.VerifiedDeadEndReturns & room.Entry) == 0 ||
                            !rooms.TryGetValue(room.Cell + RoomDirection.Offset(room.Entry), out var main) ||
                            !seen.Contains(main.Cell) || main.Branch != RoomDirection.Opposite(room.Entry))
                            errors.Add($"선택 방의 왕복 연결이 잘못됐습니다: {room.Cell}");
                    }
                    else if (room.Openings != RoomOpening.None || room.Entry != RoomOpening.None || room.Exit != RoomOpening.None || room.Branch != RoomOpening.None)
                        errors.Add($"경로 밖 방에 열린 출입구가 있습니다: {room.Cell}");
                }
                if (room.Branch != RoomOpening.None &&
                    (!rooms.TryGetValue(room.Cell + RoomDirection.Offset(room.Branch), out var branchRoom) || !branchRoom.IsOptional))
                    errors.Add($"선택 방이 없습니다: {room.Cell}");
                foreach (RoomOpening side in RoomDirection.All)
                {
                    if ((room.Openings & side) == 0) continue;
                    if (!rooms.TryGetValue(room.Cell + RoomDirection.Offset(side), out MapRoomPlan neighbor) ||
                        (neighbor.Openings & RoomDirection.Opposite(side)) == 0)
                        errors.Add($"대응하는 출입구가 없습니다: {room.Cell} {side}");
                }
            }
            return errors;
        }

        private static bool InBounds(Vector2Int cell, int width, int height) =>
            cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;

        private static bool Inside(Vector3 p) =>
            !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
            !float.IsNaN(p.z) && !float.IsInfinity(p.z) && p.x >= 0 && p.x < RoomTemplate.RoomSize &&
            p.y >= 0 && p.y < RoomTemplate.RoomSize;
    }
}
