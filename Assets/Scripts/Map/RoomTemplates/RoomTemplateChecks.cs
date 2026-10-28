namespace ProjectRE.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public static class RoomTemplateChecks
    {
        private static readonly RoomOpening[] Sides =
            { RoomOpening.North, RoomOpening.East, RoomOpening.South, RoomOpening.West };
        public static List<string> Validate(RoomTemplate template)
        {
            var errors = new List<string>();
            if (template == null)
            {
                errors.Add("템플릿이 없습니다.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(template.TemplateId)) errors.Add("템플릿 ID가 비어 있습니다.");
            if (!System.Enum.IsDefined(typeof(RoomType), template.Type)) errors.Add("알 수 없는 방 유형입니다.");
            const RoomOpening all = RoomOpening.North | RoomOpening.East | RoomOpening.South | RoomOpening.West;
            if ((template.Openings & ~all) != 0) errors.Add("알 수 없는 출입구 방향이 있습니다.");
            if (template.transform.localScale != Vector3.one || template.transform.localRotation != Quaternion.identity)
                errors.Add("루트 회전은 0, 크기는 1이어야 합니다.");

            Grid grid = template.GetComponent<Grid>();
            if (grid == null || grid.cellSize != Vector3.one || grid.cellGap != Vector3.zero)
                errors.Add("Grid 셀 크기는 1, 간격은 0이어야 합니다.");

            if (!RoomDirection.MatchesType(template.Type, template.Openings))
                errors.Add("방 유형과 출입구 방향이 일치하지 않습니다.");

            Tilemap map = template.FixedGeometry;
            if (map == null || !map.transform.IsChildOf(template.transform))
                errors.Add("고정 지형 Tilemap을 방 프리팹 내부에 지정해야 합니다.");
            else
            {
                if (map.transform.parent != template.transform || map.transform.localPosition != Vector3.zero ||
                    map.transform.localScale != Vector3.one || map.transform.localRotation != Quaternion.identity)
                    errors.Add("고정 지형 Tilemap은 방 루트의 자식이며 위치 0·회전 0·크기 1이어야 합니다.");
                var collider = map.GetComponent<TilemapCollider2D>();
                if (collider == null || !collider.enabled || collider.isTrigger)
                    errors.Add("고정 지형에 활성화된 충돌 TilemapCollider2D가 필요합니다.");

                for (int x = 0; x < RoomTemplate.RoomSize; x++)
                for (int y = 0; y < RoomTemplate.RoomSize; y++)
                {
                    if (x != 0 && x != RoomTemplate.RoomSize - 1 && y != 0 && y != RoomTemplate.RoomSize - 1)
                        continue;
                    var cell = new Vector2Int(x, y);
                    bool expectedOpen = false;
                    foreach (RoomOpening side in Sides)
                    {
                        if ((template.Openings & side) != 0 && RoomTemplate.OpeningCells(side).Contains(cell))
                            expectedOpen = true;
                    }
                    bool occupied = map.HasTile(new Vector3Int(x, y, 0));
                    if (occupied == expectedOpen)
                        errors.Add($"경계 지형과 출입구가 일치하지 않습니다: {cell}");
                }
            }

            var uniquePassages = new HashSet<(RoomOpening, RoomOpening)>();
            foreach (RoomPassage passage in template.Passages)
            {
                if (!IsSide(passage.entry) || !IsSide(passage.exit) || passage.entry == passage.exit ||
                    (template.Openings & passage.entry) == 0 || (template.Openings & passage.exit) == 0)
                    errors.Add($"잘못된 통과 방향: {passage.entry} → {passage.exit}");
                if (!uniquePassages.Add((passage.entry, passage.exit)))
                    errors.Add($"중복된 통과 방향: {passage.entry} → {passage.exit}");
            }

            var room = new RectInt(0, 0, RoomTemplate.RoomSize, RoomTemplate.RoomSize);
            for (int i = 0; i < template.VariationRegions.Count; i++)
            {
                RectInt region = template.VariationRegions[i];
                if (region.width <= 0 || region.height <= 0 || !room.Contains(region.min) ||
                    region.xMax > room.xMax || region.yMax > room.yMax)
                    errors.Add($"변형 영역이 방 밖이거나 크기가 없습니다: {region}");
                if (region.xMin < 1 || region.yMin < 1 || region.xMax > RoomTemplate.RoomSize - 1 || region.yMax > RoomTemplate.RoomSize - 1)
                    errors.Add($"변형 영역은 방 경계와 떨어져 있어야 합니다: {region}");
                for (int j = 0; j < i; j++)
                    if (region.Overlaps(template.VariationRegions[j])) errors.Add($"변형 영역이 겹칩니다: {region}");
                foreach (RoomOpening side in Sides)
                    if (region.Overlaps(RoomTemplate.OpeningCells(side)))
                        errors.Add($"변형 영역이 {side} 경계와 겹칩니다: {region}");
                foreach (RectInt protectedRegion in template.ProtectedRegions)
                    if (region.Overlaps(protectedRegion)) errors.Add($"변형 영역이 보호 구역과 겹칩니다: {region}");
            }

            foreach (RectInt protectedRegion in template.ProtectedRegions)
                if (protectedRegion.width <= 0 || protectedRegion.height <= 0 || !room.Contains(protectedRegion.min) ||
                    protectedRegion.xMax > room.xMax || protectedRegion.yMax > room.yMax)
                    errors.Add($"보호 구역이 방 밖이거나 크기가 없습니다: {protectedRegion}");

            var usedRegions = new HashSet<int>();
            foreach (RoomTerrainVariation variation in template.TerrainVariations)
            {
                if (variation == null || variation.regionIndex < 0 || variation.regionIndex >= template.VariationRegions.Count ||
                    !usedRegions.Add(variation.regionIndex))
                { errors.Add("변형 설정의 영역이 없거나 중복됩니다."); continue; }
                RectInt region = template.VariationRegions[variation.regionIndex];
                if (region.width <= 0 || region.height <= 0 || region.xMin < 1 || region.yMin < 1 ||
                    region.xMax > RoomTemplate.RoomSize - 1 || region.yMax > RoomTemplate.RoomSize - 1)
                { errors.Add("변형 설정의 영역이 올바르지 않습니다."); continue; }
                if (!variation.combinationsVerified) errors.Add($"변형 조합의 Player 통과 검증이 필요합니다: {variation.regionIndex}");
                if (!variation.allowEmptyPattern && variation.patterns.Count == 0) errors.Add("선택 가능한 패턴이 없습니다.");
                if (map != null)
                    foreach (Vector2Int cell in region.allPositionsWithin)
                        if (map.HasTile(new Vector3Int(cell.x, cell.y, 0))) errors.Add($"변형 영역이 고정 지형과 겹칩니다: {cell}");
                var cells = new HashSet<Vector2Int>();
                foreach (RoomChanceCell chance in variation.chanceCells)
                    if (chance == null || !region.Contains(chance.cell) || chance.tile == null || chance.chance < 0 || chance.chance > 100 || !cells.Add(chance.cell))
                        errors.Add("확률 셀의 위치, 타일, 확률 또는 중복이 잘못됐습니다.");
                var patternIds = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (RoomTerrainPattern pattern in variation.patterns)
                {
                    if (pattern == null || string.IsNullOrWhiteSpace(pattern.id) || !patternIds.Add(pattern.id) || pattern.prefab == null)
                    { errors.Add("패턴 ID가 비어 있거나 중복되었거나 프리팹이 없습니다."); continue; }
                    var maps = pattern.prefab.GetComponentsInChildren<Tilemap>(true);
                    Grid patternGrid = pattern.prefab.GetComponent<Grid>();
                    if (!pattern.prefab.activeSelf || patternGrid == null || patternGrid.cellSize != Vector3.one ||
                        patternGrid.cellGap != Vector3.zero || maps.Length != 1 ||
                        maps[0].transform.parent != pattern.prefab.transform || maps[0].transform.localPosition != Vector3.zero ||
                        maps[0].transform.localRotation != Quaternion.identity || maps[0].transform.localScale != Vector3.one ||
                        pattern.prefab.transform.localRotation != Quaternion.identity || pattern.prefab.transform.localScale != Vector3.one)
                    { errors.Add($"패턴의 Grid 또는 Tilemap 구성이 잘못됐습니다: {pattern.id}"); continue; }
                    var patternCollider = maps[0].GetComponent<TilemapCollider2D>();
                    if (!maps[0].gameObject.activeSelf || patternCollider == null || !patternCollider.enabled || patternCollider.isTrigger)
                        errors.Add($"패턴에 활성화된 충돌 TilemapCollider2D가 필요합니다: {pattern.id}");
                    if (map != null && maps[0].gameObject.layer != map.gameObject.layer)
                        errors.Add($"패턴과 고정 지형의 레이어가 다릅니다: {pattern.id}");
                    foreach (Collider2D collider in pattern.prefab.GetComponentsInChildren<Collider2D>(true))
                        if (collider.gameObject != maps[0].gameObject ||
                            !(collider is TilemapCollider2D || collider is CompositeCollider2D) ||
                            !collider.enabled || collider.isTrigger || collider.offset != Vector2.zero)
                            errors.Add($"패턴에 영역을 벗어날 수 있는 Collider가 있습니다: {pattern.id}");
                    int tileCount = 0;
                    foreach (Vector3Int position in maps[0].cellBounds.allPositionsWithin)
                    {
                        if (!maps[0].HasTile(position)) continue;
                        tileCount++;
                        var cell = new Vector2Int(region.xMin + position.x, region.yMin + position.y);
                        if (position.z != 0 || !region.Contains(cell) || cells.Contains(cell))
                            errors.Add($"패턴이 영역 밖 또는 확률 셀과 겹칩니다: {pattern.id} {cell}");
                    }
                    if (tileCount == 0) errors.Add($"패턴에 타일이 없습니다: {pattern.id}");
                }
            }

            foreach (Vector2Int slot in template.ContentSlots)
            {
                if (!room.Contains(slot)) errors.Add($"콘텐츠 슬롯이 방 밖입니다: {slot}");
                else if (map != null && map.HasTile(new Vector3Int(slot.x, slot.y, 0)))
                    errors.Add($"콘텐츠 슬롯이 고정 지형과 겹칩니다: {slot}");
            }
            ValidateContent(template, map, room, errors);

            if (template.PlayerStart != null && !template.PlayerStart.IsChildOf(template.transform))
                errors.Add("Player 시작 마커가 방 내부에 없습니다.");
            if (template.Goal != null && !template.Goal.IsChildOf(template.transform))
                errors.Add("Goal 마커가 방 내부에 없습니다.");
            CheckMarker(template, template.PlayerStart, "Player 시작", errors);
            CheckMarker(template, template.Goal, "Goal", errors);
            if ((template.VerifiedStartExits & ~template.Openings) != 0 ||
                (template.VerifiedStartExits != RoomOpening.None && template.PlayerStart == null))
                errors.Add("시작 마커의 검증 방향이 출입구 또는 마커와 일치하지 않습니다.");
            if ((template.VerifiedGoalEntries & ~template.Openings) != 0 ||
                (template.VerifiedGoalEntries != RoomOpening.None && template.Goal == null))
                errors.Add("Goal 마커의 검증 방향이 출입구 또는 마커와 일치하지 않습니다.");
            if ((template.VerifiedDeadEndReturns & ~template.Openings) != 0 ||
                (template.VerifiedDeadEndReturns != RoomOpening.None &&
                 (template.Openings != template.VerifiedDeadEndReturns || !IsSide(template.Openings) || template.OptionalTarget == null)))
                errors.Add("막다른 방의 왕복 검증 방향이 출입구와 일치하지 않습니다.");
            if (template.OptionalTarget != null && !template.OptionalTarget.IsChildOf(template.transform))
                errors.Add("선택 방 목표 마커가 방 내부에 없습니다.");
            CheckMarker(template, template.OptionalTarget, "선택 방 목표", errors);
            return errors;
        }

        private static void ValidateContent(RoomTemplate template, Tilemap map, RectInt room, List<string> errors)
        {
            var indices = new HashSet<int>();
            var usedAreas = new List<RectInt>();
            foreach (RoomContentSlot slot in template.ContentSettings)
            {
                if (slot == null || slot.slotIndex < 0 || slot.slotIndex >= template.ContentSlots.Count || !indices.Add(slot.slotIndex))
                { errors.Add("콘텐츠 설정의 슬롯이 없거나 중복됩니다."); continue; }
                RectInt area = slot.clearance;
                if (area.width <= 0 || area.height <= 0 || !room.Contains(area.min) || area.xMax > room.xMax || area.yMax > room.yMax ||
                    !area.Contains(template.ContentSlots[slot.slotIndex]) || !System.Enum.IsDefined(typeof(ContentSurface), slot.surface) ||
                    !System.Enum.IsDefined(typeof(ContentFacing), slot.facing))
                { errors.Add("콘텐츠 공간 또는 방향이 잘못됐습니다."); continue; }
                foreach (RectInt other in usedAreas) if (area.Overlaps(other)) errors.Add("콘텐츠 슬롯의 충돌 공간이 겹칩니다.");
                usedAreas.Add(area);
                foreach (RectInt protectedRegion in template.ProtectedRegions)
                    if (area.Overlaps(protectedRegion)) errors.Add("콘텐츠 공간이 보호 구역과 겹칩니다.");
                foreach (RoomOpening side in Sides)
                    if (area.Overlaps(RoomTemplate.OpeningCells(side))) errors.Add("콘텐츠 공간이 출입구와 겹칩니다.");
                RectInt support;
                switch (slot.surface)
                {
                    case ContentSurface.Floor: support = new RectInt(area.xMin, area.yMin - 1, area.width, 1); break;
                    case ContentSurface.Ceiling: support = new RectInt(area.xMin, area.yMax, area.width, 1); break;
                    case ContentSurface.WestWall: support = new RectInt(area.xMin - 1, area.yMin, 1, area.height); break;
                    default: support = new RectInt(area.xMax, area.yMin, 1, area.height); break;
                }
                foreach (RectInt region in template.VariationRegions)
                    if (area.Overlaps(region) || support.Overlaps(region)) errors.Add("콘텐츠 공간 또는 지지면이 변형 영역과 겹칩니다.");
                if (map != null)
                {
                    foreach (Vector2Int p in area.allPositionsWithin)
                        if (map.HasTile(new Vector3Int(p.x, p.y, 0))) errors.Add($"콘텐츠 공간에 고정 지형이 있습니다: {p}");
                    foreach (Vector2Int p in support.allPositionsWithin)
                        if (!room.Contains(p) || !map.HasTile(new Vector3Int(p.x, p.y, 0)) ||
                            map.GetColliderType(new Vector3Int(p.x, p.y, 0)) != Tile.ColliderType.Grid)
                            errors.Add($"콘텐츠 지지면에 전체 셀 충돌이 없습니다: {p}");
                }
                long total = slot.emptyWeight;
                if (slot.emptyWeight < 0) errors.Add("빈 슬롯 가중치가 음수입니다.");
                var ids = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (RoomContentOption option in slot.options)
                {
                    if (option == null || string.IsNullOrWhiteSpace(option.id) || !ids.Add(option.id) || option.prefab == null || option.weight <= 0)
                    { errors.Add("콘텐츠 ID, 프리팹 또는 가중치가 올바르지 않습니다."); continue; }
                    total += option.weight;
                    var prefab = option.prefab;
                    var colliders = prefab.GetComponentsInChildren<Collider2D>(true);
                    if (!prefab.activeSelf || prefab.transform.localRotation != Quaternion.identity || colliders.Length != 1 ||
                        colliders[0].gameObject != prefab || !colliders[0].enabled ||
                        !(colliders[0] is BoxCollider2D || colliders[0] is CapsuleCollider2D))
                    { errors.Add($"콘텐츠의 루트 Collider가 올바르지 않습니다: {option.id}"); continue; }
                    Vector2 size = colliders[0] is BoxCollider2D box ? box.size : ((CapsuleCollider2D)colliders[0]).size;
                    Vector3 scale = prefab.transform.localScale;
                    if (float.IsNaN(option.offset.x) || float.IsInfinity(option.offset.x) || float.IsNaN(option.offset.y) || float.IsInfinity(option.offset.y) ||
                        float.IsNaN(scale.x) || float.IsInfinity(scale.x) || float.IsNaN(scale.y) || float.IsInfinity(scale.y) || scale.y <= 0 || scale.x <= 0)
                    { errors.Add($"콘텐츠 위치 또는 크기가 올바르지 않습니다: {option.id}"); continue; }
                    float sx = Mathf.Abs(scale.x), sy = Mathf.Abs(scale.y), facing = slot.facing == ContentFacing.Left ? -1f : 1f;
                    Vector2 origin = (Vector2)template.ContentSlots[slot.slotIndex] + new Vector2(.5f, 0) + option.offset;
                    Vector2 center = origin + new Vector2(colliders[0].offset.x * sx * facing, colliders[0].offset.y * sy);
                    Vector2 half = new Vector2(size.x * sx, size.y * sy) * .5f;
                    const float epsilon = .02f;
                    if (sx <= 0 || sy <= 0 || center.x - half.x < area.xMin - epsilon || center.x + half.x > area.xMax + epsilon ||
                        center.y - half.y < area.yMin - epsilon || center.y + half.y > area.yMax + epsilon ||
                        (slot.surface == ContentSurface.Floor && Mathf.Abs(center.y - half.y - area.yMin) > epsilon) ||
                        (slot.surface == ContentSurface.Ceiling && Mathf.Abs(center.y + half.y - area.yMax) > epsilon) ||
                        (slot.surface == ContentSurface.WestWall && Mathf.Abs(center.x - half.x - area.xMin) > epsilon) ||
                        (slot.surface == ContentSurface.EastWall && Mathf.Abs(center.x + half.x - area.xMax) > epsilon))
                        errors.Add($"콘텐츠 Collider가 공간이나 지지면에 맞지 않습니다: {option.id}");
                }
                if (total <= 0 || total > 1000000) errors.Add("콘텐츠 전체 가중치가 올바르지 않습니다.");
            }
        }

        private static void CheckMarker(RoomTemplate template, Transform marker, string label, List<string> errors)
        {
            if (marker == null) return;
            Vector3 pos = template.transform.InverseTransformPoint(marker.position);
            if (float.IsNaN(pos.x) || float.IsInfinity(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.y) ||
                pos.x < 0 || pos.x >= RoomTemplate.RoomSize || pos.y < 0 || pos.y >= RoomTemplate.RoomSize)
                errors.Add($"{label} 마커가 방 범위 밖입니다.");
        }

        private static bool IsSide(RoomOpening opening)
        {
            foreach (RoomOpening side in Sides)
                if (opening == side) return true;
            return false;
        }
    }
}
