namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>방 계획의 지형 패턴과 확률 셀을 적용한다.</summary>
    internal static class RoomTerrainBuilder
    {
        public static void Apply(MapRoomPlan room, RoomTemplate template, Vector3Int roomOrigin,
            List<MapVisualBuilder.TerrainMap> terrainMaps, bool buildVisual, MapBuildContext context)
        {
            var settings = template.Terrain;
            if (buildVisual) terrainMaps.Add(new MapVisualBuilder.TerrainMap(template.FixedGeometry, roomOrigin));
            foreach (RoomTerrainSelection selection in room.Terrain)
            {
                RoomTerrainVariation variation = null;
                foreach (RoomTerrainVariation candidate in settings.TerrainVariations)
                    if (candidate.regionIndex == selection.RegionIndex) { variation = candidate; break; }
                if (variation == null) throw new InvalidOperationException("변형 영역을 찾을 수 없습니다.");
                RectInt region = settings.VariationRegions[selection.RegionIndex];
                if (!string.IsNullOrEmpty(selection.PatternId))
                {
                    RoomTerrainPattern selectedPattern = null;
                    foreach (RoomTerrainPattern pattern in variation.patterns)
                        if (pattern.id == selection.PatternId) { selectedPattern = pattern; break; }
                    if (selectedPattern == null || selectedPattern.prefab == null)
                        throw new InvalidOperationException($"패턴을 찾을 수 없습니다: {selection.PatternId}");
                    GameObject instance = context.InstantiatePrefab(selectedPattern.prefab, template.transform);
                    if (instance == null) throw new InvalidOperationException($"패턴 생성 실패: {selection.PatternId}");
                    Vector2 offset = selectedPattern.kind == RoomTerrainPatternKind.Platform ? selectedPattern.offset : Vector2.zero;
                    instance.transform.localPosition = new Vector3(region.xMin + offset.x, region.yMin + offset.y, 0);
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    if (buildVisual && selectedPattern.kind == RoomTerrainPatternKind.Tilemap)
                    {
                        var patternMap = instance.GetComponentInChildren<Tilemap>(true);
                        terrainMaps.Add(new MapVisualBuilder.TerrainMap(patternMap, roomOrigin + new Vector3Int(region.xMin, region.yMin, 0)));
                    }
                }
                if (selection.FilledCells.Count == 0) continue;
                if (context.RecordComponent == null) throw new InvalidOperationException("타일 기록 컨텍스트가 없습니다.");
                context.RecordComponent(template.FixedGeometry);
                foreach (Vector2Int cell in selection.FilledCells)
                    foreach (RoomChanceCell chance in variation.chanceCells)
                        if (chance.cell == cell) { template.FixedGeometry.SetTile(new Vector3Int(cell.x, cell.y, 0), chance.tile); break; }
            }
        }
    }
}
