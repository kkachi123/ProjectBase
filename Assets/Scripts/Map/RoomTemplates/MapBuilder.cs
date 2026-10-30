namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public sealed class MapBuildResult
    {
        public bool Success => Root != null;
        public GameObject Root { get; }
        public MapPlan Plan { get; }
        public string Error { get; }

        private MapBuildResult(GameObject root, MapPlan plan, string error)
        { Root = root; Plan = plan; Error = error; }

        public static MapBuildResult Succeeded(GameObject root, MapPlan plan) => new(root, plan, null);
        public static MapBuildResult Failed(string error) => new(null, null, error);
    }

    /// <summary>검증된 계획을 비활성 루트에 생성한다. 기존 맵은 다루지 않는다.</summary>
    public static class MapBuilder
    {
        public static MapBuildResult Build(MapPlan plan, RoomTemplateCatalog catalog,
            GameObject goalPrefab, MapBuildContext context = null, TileBase visualGround = null,
            Material visualGroundMaterial = null)
        {
            if (catalog == null || goalPrefab == null) return MapBuildResult.Failed("카탈로그 또는 Goal 프리팹이 없습니다.");
            if (context == null) context = new MapBuildContext();
            if (context.CreateRoot == null || context.InstantiatePrefab == null || context.DestroyObject == null)
                return MapBuildResult.Failed("맵 생성 컨텍스트가 올바르지 않습니다.");

            if (!catalog.TryCreateSnapshot(out var snapshots, out string catalogError))
                return MapBuildResult.Failed(catalogError);
            var errors = MapPlanValidator.Validate(plan, snapshots);
            if (errors.Count > 0) return MapBuildResult.Failed(string.Join("; ", errors));
            var prefabs = new Dictionary<string, RoomTemplate>(StringComparer.Ordinal);
            foreach (RoomTemplate template in catalog.Templates)
            {
                if (template == null || string.IsNullOrWhiteSpace(template.TemplateId) || prefabs.ContainsKey(template.TemplateId))
                    return MapBuildResult.Failed("카탈로그의 템플릿 또는 ID가 올바르지 않습니다.");
                prefabs.Add(template.TemplateId, template);
            }
            foreach (MapRoomPlan room in plan.Rooms)
                if (!prefabs.ContainsKey(room.TemplateId))
                    return MapBuildResult.Failed($"템플릿을 찾을 수 없습니다: {room.TemplateId}");

            GameObject root = null;
            try
            {
                root = context.CreateRoot($"RoomMap seed={plan.Seed}");
                if (root == null) return MapBuildResult.Failed("맵 루트를 생성하지 못했습니다.");
                root.SetActive(false);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one;
                var terrainMaps = new List<MapVisualBuilder.TerrainMap>();
                foreach (MapRoomPlan room in plan.Rooms)
                {
                    GameObject instance = context.InstantiatePrefab(prefabs[room.TemplateId].gameObject, root.transform);
                    if (instance == null) throw new InvalidOperationException($"방 생성 실패: {room.TemplateId}");
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.localPosition = RoomDirection.Origin(room.Cell);
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    instance.name = $"Room ({room.Cell.x}, {room.Cell.y}) {room.TemplateId}";
                    var template = instance.GetComponent<RoomTemplate>();
                    var roomOrigin = new Vector3Int(room.Cell.x * RoomTemplate.RoomSize, -room.Cell.y * RoomTemplate.RoomSize, 0);
                    if (visualGround != null) terrainMaps.Add(new MapVisualBuilder.TerrainMap(template.FixedGeometry, roomOrigin));
                    foreach (RoomTerrainSelection selection in room.Terrain)
                    {
                        RoomTerrainVariation variation = null;
                        foreach (RoomTerrainVariation candidate in template.TerrainVariations)
                            if (candidate.regionIndex == selection.RegionIndex) { variation = candidate; break; }
                        if (variation == null) throw new InvalidOperationException("변형 영역을 찾을 수 없습니다.");
                        RectInt region = template.VariationRegions[selection.RegionIndex];
                        if (!string.IsNullOrEmpty(selection.PatternId))
                        {
                            GameObject patternPrefab = null;
                            foreach (RoomTerrainPattern pattern in variation.patterns)
                                if (pattern.id == selection.PatternId) { patternPrefab = pattern.prefab; break; }
                            if (patternPrefab == null) throw new InvalidOperationException($"패턴을 찾을 수 없습니다: {selection.PatternId}");
                            GameObject patternInstance = context.InstantiatePrefab(patternPrefab, instance.transform);
                            if (patternInstance == null) throw new InvalidOperationException($"패턴 생성 실패: {selection.PatternId}");
                            patternInstance.transform.localPosition = new Vector3(region.xMin, region.yMin, 0);
                            patternInstance.transform.localRotation = Quaternion.identity;
                            patternInstance.transform.localScale = Vector3.one;
                            if (visualGround != null)
                            {
                                var patternMap = patternInstance.GetComponentInChildren<Tilemap>(true);
                                terrainMaps.Add(new MapVisualBuilder.TerrainMap(patternMap, roomOrigin + new Vector3Int(region.xMin, region.yMin, 0)));
                            }
                        }
                        if (selection.FilledCells.Count > 0)
                        {
                            if (context.RecordComponent == null) throw new InvalidOperationException("타일 기록 컨텍스트가 없습니다.");
                            context.RecordComponent(template.FixedGeometry);
                            foreach (Vector2Int cell in selection.FilledCells)
                                foreach (RoomChanceCell chance in variation.chanceCells)
                                    if (chance.cell == cell) { template.FixedGeometry.SetTile(new Vector3Int(cell.x, cell.y, 0), chance.tile); break; }
                        }
                    }
                    foreach (ContentSelection selection in room.Content)
                    {
                        if (string.IsNullOrEmpty(selection.OptionId)) continue;
                        RoomContentSlot slot = null;
                        foreach (RoomContentSlot candidate in template.ContentSettings)
                            if (candidate.slotIndex == selection.SlotIndex) { slot = candidate; break; }
                        if (slot == null) throw new InvalidOperationException("콘텐츠 슬롯을 찾을 수 없습니다.");
                        RoomContentOption option = null;
                        foreach (RoomContentOption candidate in slot.options)
                            if (candidate.id == selection.OptionId) { option = candidate; break; }
                        if (option == null || option.prefab == null) throw new InvalidOperationException($"콘텐츠를 찾을 수 없습니다: {selection.OptionId}");
                        GameObject content = context.InstantiatePrefab(option.prefab, instance.transform);
                        if (content == null) throw new InvalidOperationException($"콘텐츠 생성 실패: {selection.OptionId}");
                        Vector2Int point = template.ContentSlots[slot.slotIndex];
                        content.transform.localPosition = new Vector3(point.x + .5f + option.offset.x, point.y + option.offset.y, 0);
                        content.transform.localRotation = Quaternion.identity;
                        Vector3 scale = content.transform.localScale;
                        scale.x = Mathf.Abs(scale.x) * (slot.facing == ContentFacing.Left ? -1 : 1);
                        content.transform.localScale = scale;
                    }
                }
                GameObject goal = context.InstantiatePrefab(goalPrefab, root.transform);
                if (goal == null) throw new InvalidOperationException("Goal 생성 실패");
                goal.transform.SetParent(root.transform, false);
                goal.transform.localPosition = plan.Goal;
                goal.transform.localRotation = Quaternion.identity;
                if (visualGround != null) MapVisualBuilder.Build(root, terrainMaps, visualGround, context, visualGroundMaterial);
                return MapBuildResult.Succeeded(root, plan);
            }
            catch (Exception exception)
            {
                if (root != null) context.DestroyObject(root);
                return MapBuildResult.Failed($"맵 생성 실패: {exception.Message}");
            }
        }
    }
}
