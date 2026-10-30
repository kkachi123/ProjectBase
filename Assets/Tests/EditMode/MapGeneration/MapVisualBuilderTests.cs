namespace ProjectRE.MapGeneration.Tests
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public sealed class MapVisualBuilderTests
    {
        [Test]
        public void MergesSourceCellsAcrossNegativeRoomAndPatternOffsetsWithoutChangingCollision()
        {
            var root = new GameObject("map");
            var ground = ScriptableObject.CreateInstance<Tile>();
            ground.colliderType = Tile.ColliderType.Grid;
            var visualTile = ScriptableObject.CreateInstance<Tile>();
            visualTile.colliderType = Tile.ColliderType.None;
            try
            {
                var fixedMap = CreateMap(root.transform, "fixed");
                var patternMap = CreateMap(root.transform, "pattern");
                fixedMap.SetTile(new Vector3Int(19, 4, 0), ground);
                patternMap.SetTile(new Vector3Int(2, 1, 0), ground);
                var sources = new[] {
                    new MapVisualBuilder.TerrainMap(fixedMap, new Vector3Int(0, -20, 0)),
                    new MapVisualBuilder.TerrainMap(patternMap, new Vector3Int(18, -17, 0))
                };
                MapVisualBuilder.Build(root, sources, visualTile, new MapBuildContext());
                var result = root.transform.Find("Terrain Visual Grid/Terrain Visual").GetComponent<Tilemap>();
                Assert.That(result.GetTile(new Vector3Int(19, -16, 0)), Is.SameAs(visualTile));
                Assert.That(result.GetTile(new Vector3Int(20, -16, 0)), Is.SameAs(visualTile));
                Assert.That(result.GetTile(new Vector3Int(19, -16, 0) + Vector3Int.up), Is.Null);
                Assert.That(fixedMap.GetColliderType(new Vector3Int(19, 4, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                Assert.That(fixedMap.GetComponent<TilemapRenderer>().enabled, Is.False);
                Assert.That(patternMap.GetComponent<TilemapRenderer>().enabled, Is.False);
                Assert.That(result.GetComponent<TilemapCollider2D>(), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(visualTile); }
        }

        [TestCase(Tile.ColliderType.None)]
        [TestCase(Tile.ColliderType.Sprite)]
        public void UnsupportedCollisionLeavesSourceRendererUntouched(Tile.ColliderType collider)
        {
            var root = new GameObject("map");
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.colliderType = collider;
            try
            {
                var source = CreateMap(root.transform, "source");
                source.SetTile(Vector3Int.zero, tile);
                Assert.Throws<InvalidOperationException>(() => MapVisualBuilder.Build(root,
                    new[] { new MapVisualBuilder.TerrainMap(source, Vector3Int.zero) }, tile, new MapBuildContext()));
                Assert.That(source.GetComponent<TilemapRenderer>().enabled, Is.True);
                Assert.That(root.transform.Find("Terrain Visual Grid"), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(tile); }
        }

        [Test]
        public void OverlappingTerrainFailsBeforeSourceRenderersChange()
        {
            var root = new GameObject("map");
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.colliderType = Tile.ColliderType.Grid;
            try
            {
                var a = CreateMap(root.transform, "a");
                var b = CreateMap(root.transform, "b");
                a.SetTile(Vector3Int.right, tile);
                b.SetTile(Vector3Int.zero, tile);
                Assert.Throws<InvalidOperationException>(() => MapVisualBuilder.Build(root,
                    new[] { new MapVisualBuilder.TerrainMap(a, Vector3Int.zero), new MapVisualBuilder.TerrainMap(b, Vector3Int.right) }, tile, new MapBuildContext()));
                Assert.That(a.GetComponent<TilemapRenderer>().enabled && b.GetComponent<TilemapRenderer>().enabled, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(tile); }
        }

        private static Tilemap CreateMap(Transform parent, string name)
        {
            var grid = new GameObject(name + " Grid", typeof(Grid));
            grid.transform.SetParent(parent, false);
            var map = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            map.transform.SetParent(grid.transform, false);
            return map.GetComponent<Tilemap>();
        }
    }
}
