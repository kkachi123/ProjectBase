namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>확정된 방 지형을 충돌 없이 하나의 그래픽 Tilemap으로 표시한다.</summary>
    internal static class MapVisualBuilder
    {
        internal readonly struct TerrainMap
        {
            public readonly Tilemap Map;
            public readonly Vector3Int Origin;
            public TerrainMap(Tilemap map, Vector3Int origin) { Map = map; Origin = origin; }
        }

        internal static void Build(GameObject root, IReadOnlyList<TerrainMap> maps, TileBase visualGround,
            MapBuildContext context, Material visualGroundMaterial = null)
        {
            if (context.RecordComponent == null) throw new InvalidOperationException("그래픽 변경 기록 컨텍스트가 없습니다.");
            var occupied = new HashSet<Vector3Int>();
            var renderers = new List<TilemapRenderer>();
            foreach (TerrainMap source in maps)
            {
                if (source.Map == null) throw new InvalidOperationException("그래픽 지형 Tilemap이 없습니다.");
                var renderer = source.Map.GetComponent<TilemapRenderer>();
                if (renderer == null) throw new InvalidOperationException($"지형 Renderer가 없습니다: {source.Map.name}");
                renderers.Add(renderer);
                foreach (Vector3Int local in source.Map.cellBounds.allPositionsWithin)
                {
                    if (!source.Map.HasTile(local)) continue;
                    if (local.z != 0 || source.Map.GetColliderType(local) != Tile.ColliderType.Grid)
                        throw new InvalidOperationException($"Grid 충돌 지형만 그래픽으로 표시할 수 있습니다: {source.Map.name} {local}");
                    var cell = source.Origin + local;
                    if (!occupied.Add(cell)) throw new InvalidOperationException($"지형 셀이 중복됩니다: {cell}");
                }
            }

            var gridObject = context.CreateRoot("Terrain Visual Grid");
            if (gridObject == null) throw new InvalidOperationException("그래픽 Grid 생성 실패");
            gridObject.transform.SetParent(root.transform, false);
            context.AddComponent(gridObject, typeof(Grid));
            var tileObject = context.CreateRoot("Terrain Visual");
            if (tileObject == null) throw new InvalidOperationException("그래픽 Tilemap 생성 실패");
            tileObject.transform.SetParent(gridObject.transform, false);
            var visual = (Tilemap)context.AddComponent(tileObject, typeof(Tilemap));
            var visualRenderer = (TilemapRenderer)context.AddComponent(tileObject, typeof(TilemapRenderer));
            if (visual == null) throw new InvalidOperationException("그래픽 Tilemap 구성 실패");
            if (visualRenderer == null) throw new InvalidOperationException("그래픽 Tilemap Renderer 구성 실패");
            if (visualGroundMaterial != null)
            {
                context.RecordComponent(visualRenderer);
                visualRenderer.sharedMaterial = visualGroundMaterial;
            }
            context.RecordComponent(visual);
            foreach (Vector3Int cell in occupied) visual.SetTile(cell, visualGround);
            visual.RefreshAllTiles();
            foreach (TilemapRenderer renderer in renderers)
            {
                context.RecordComponent(renderer);
                renderer.enabled = false;
            }
        }
    }
}
