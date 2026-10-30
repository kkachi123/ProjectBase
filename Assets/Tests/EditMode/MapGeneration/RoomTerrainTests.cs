namespace ProjectRE.MapGeneration.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class RoomTerrainTests
    {
        private const string SettingsPath = "Assets/Prefabs/@Data/Map/MapPlanningSettings.asset";
        private const string GoalPath = "Assets/Prefabs/Map/GridChunk/@BackGroundPrefab/GoalTrigger/EndLight-EndTrigger.prefab";

        [TestCase(-1, false)]
        [TestCase(16, true)]
        public void UnityEverythingFlagIsAcceptedButUnknownOpeningBitsAreRejected(int value, bool invalid)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Base/DownThrough.prefab");
            var room = Object.Instantiate(prefab).GetComponent<RoomTemplate>();
            try
            {
                var serialized = new SerializedObject(room);
                serialized.FindProperty("_openings").intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var errors = RoomTemplateChecks.Validate(room);
                Assert.That(errors.Any(e => e.Contains("알 수 없는 출입구 방향")), Is.EqualTo(invalid));
                if (!invalid) Assert.That(room.Openings, Is.EqualTo(RoomOpening.North | RoomOpening.East | RoomOpening.South | RoomOpening.West));
            }
            finally { Object.DestroyImmediate(room.gameObject); }
        }

        [Test]
        public void VariationUsesIndependentDeterministicSeedAndProducesEveryCombination()
        {
            var region = new RoomTerrainSnapshot(0, true, new[] { "platform" }, new[] { new Vector2Int(3, 12) }, new[] { 50 });
            var template = new RoomTemplateSnapshot("test", RoomType.Horizontal, RoomOpening.East | RoomOpening.West,
                null, false, terrain: new[] { region });
            var combinations = new HashSet<string>();
            for (int seed = 0; seed < 256; seed++)
            {
                var a = RoomTerrainPlanner.Select(seed, new Vector2Int(1, 2), template)[0];
                var b = RoomTerrainPlanner.Select(seed, new Vector2Int(1, 2), template)[0];
                Assert.That(a.PatternId, Is.EqualTo(b.PatternId));
                Assert.That(a.FilledCells, Is.EqualTo(b.FilledCells));
                combinations.Add(a.PatternId + ":" + a.FilledCells.Count);
            }
            Assert.That(combinations.Count, Is.EqualTo(4));
        }

        [Test]
        public void RealPlansBuildTheRecordedPatternAndChanceCell()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var combinations = new HashSet<string>();
            for (int seed = 0; seed < 100 && combinations.Count < 4; seed++)
            {
                var plan = settings.CreatePlan(seed);
                Assert.That(plan.Success, Is.True, plan.Error);
                if (!plan.Plan.Rooms.Any(r => r.Terrain.Count > 0)) continue;
                var result = MapBuilder.Build(plan.Plan, settings.Catalog, goal);
                Assert.That(result.Success, Is.True, result.Error);
                try
                {
                    for (int i = 0; i < plan.Plan.Rooms.Count; i++)
                    {
                        MapRoomPlan room = plan.Plan.Rooms[i];
                        if (room.Terrain.Count == 0) continue;
                        var instance = result.Root.transform.GetChild(i).GetComponent<RoomTemplate>();
                        RoomTerrainSelection selected = room.Terrain[0];
                        Assert.That(instance.transform.childCount, Is.GreaterThanOrEqualTo(1));
                        bool patternExists = instance.GetComponentsInChildren<Grid>(true).Length > 1;
                        Assert.That(patternExists, Is.EqualTo(selected.PatternId != ""));
                        Assert.That(instance.FixedGeometry.HasTile(new Vector3Int(3, 12, 0)), Is.EqualTo(selected.FilledCells.Count > 0));
                        combinations.Add(selected.PatternId + ":" + selected.FilledCells.Count);
                    }
                }
                finally { Object.DestroyImmediate(result.Root); }
            }
            Assert.That(combinations.Count, Is.EqualTo(4), "실제 카탈로그의 네 조합이 모두 생성돼야 합니다.");
        }

        [Test]
        public void InvalidRegionAndUnverifiedCombinationsAreRejected()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Base/Horizontal.prefab");
            var room = Object.Instantiate(prefab).GetComponent<RoomTemplate>();
            try
            {
                var serialized = new SerializedObject(room);
                serialized.FindProperty("_terrainVariations").GetArrayElementAtIndex(0).FindPropertyRelative("combinationsVerified").boolValue = false;
                serialized.FindProperty("_protectedRegions").GetArrayElementAtIndex(0).rectIntValue = room.VariationRegions[0];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var errors = RoomTemplateChecks.Validate(room);
                Assert.That(errors.Any(e => e.Contains("보호 구역")), Is.True);
                Assert.That(errors.Any(e => e.Contains("검증")), Is.True);
            }
            finally { Object.DestroyImmediate(room.gameObject); }
        }

        [Test]
        public void AddingTerrainDoesNotChangePathOrRoomSelection()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var withTerrain, out var error), Is.True, error);
            var withoutTerrain = withTerrain.Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, t.PassagesVerified, t.HasPlayerStart, t.PlayerStart, t.HasGoal, t.Goal,
                t.VerifiedStartExits, t.VerifiedGoalEntries, verifiedDeadEndReturns: t.VerifiedDeadEndReturns,
                content: t.Content, baseGroupId: t.BaseGroupId)).ToList();
            for (int seed = 0; seed < 100; seed++)
            {
                var a = MapPlanner.Create(4, 4, seed, 32, withTerrain);
                var b = MapPlanner.Create(4, 4, seed, 32, withoutTerrain);
                Assert.That(a.Success && b.Success, Is.True);
                Assert.That(a.Plan.Path, Is.EqualTo(b.Plan.Path));
                Assert.That(a.Plan.Rooms.Select(r => r.TemplateId), Is.EqualTo(b.Plan.Rooms.Select(r => r.TemplateId)));
            }
        }

        [Test]
        public void NovaVariantInheritsVerifiedHorizontalTerrain()
        {
            var parent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Base/Horizontal.prefab");
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/NovaVariants/Horizontal_Shrine.prefab");
            Assert.That(parent, Is.Not.Null);
            Assert.That(variant, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetType(variant), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(variant), Is.SameAs(parent));
            Assert.That(PrefabUtility.GetPropertyModifications(variant).Any(m => m.target is CompositeCollider2D), Is.False,
                "부모 충돌체의 변경을 가리는 Variant 덮어쓰기가 없어야 합니다.");

            var baseRoom = parent.GetComponent<RoomTemplate>();
            var room = variant.GetComponent<RoomTemplate>();
            Assert.That(room.TemplateId, Is.Not.EqualTo(baseRoom.TemplateId));
            Assert.That(room.BaseGroupId, Is.EqualTo(baseRoom.BaseGroupId));
            Assert.That(room.Openings, Is.EqualTo(baseRoom.Openings));
            Assert.That(room.PassagesVerified, Is.True);
            Assert.That(room.Passages.Count, Is.EqualTo(baseRoom.Passages.Count));
            for (int i = 0; i < baseRoom.Passages.Count; i++)
            {
                Assert.That(room.Passages[i].entry, Is.EqualTo(baseRoom.Passages[i].entry));
                Assert.That(room.Passages[i].exit, Is.EqualTo(baseRoom.Passages[i].exit));
            }
            Assert.That(room.VariationRegions, Is.EqualTo(baseRoom.VariationRegions));
            Assert.That(room.TerrainVariations.Count, Is.EqualTo(1));
            Assert.That(room.TerrainVariations[0].combinationsVerified, Is.True);
            Assert.That(room.TerrainVariations[0].patterns[0].prefab, Is.SameAs(baseRoom.TerrainVariations[0].patterns[0].prefab));
            Assert.That(room.TerrainVariations[0].chanceCells[0].chance, Is.EqualTo(baseRoom.TerrainVariations[0].chanceCells[0].chance));
            for (int x = 0; x < RoomTemplate.RoomSize; x++)
            for (int y = 0; y < RoomTemplate.RoomSize; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                Assert.That(room.FixedGeometry.GetTile(cell), Is.SameAs(baseRoom.FixedGeometry.GetTile(cell)), $"Tile {cell}");
                Assert.That(room.FixedGeometry.GetColliderType(cell), Is.EqualTo(baseRoom.FixedGeometry.GetColliderType(cell)), $"Collider {cell}");
            }
            var art = variant.transform.Find("BackgroundArt");
            Assert.That(art, Is.Not.Null);
            Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(RoomTemplateChecks.Validate(room), Is.Empty);

            var baseInstance = Object.Instantiate(parent);
            var variantInstance = Object.Instantiate(variant);
            try
            {
                Physics2D.SyncTransforms();
                var baseCollider = baseInstance.transform.Find("FixedGeometry").GetComponent<CompositeCollider2D>();
                var variantCollider = variantInstance.transform.Find("FixedGeometry").GetComponent<CompositeCollider2D>();
                Assert.That(baseCollider.pathCount, Is.GreaterThan(0));
                Assert.That(variantCollider.pathCount, Is.EqualTo(baseCollider.pathCount));
                for (int i = 0; i < baseCollider.pathCount; i++)
                {
                    var basePoints = new Vector2[baseCollider.GetPathPointCount(i)];
                    var variantPoints = new Vector2[variantCollider.GetPathPointCount(i)];
                    baseCollider.GetPath(i, basePoints);
                    variantCollider.GetPath(i, variantPoints);
                    Assert.That(variantPoints, Is.EqualTo(basePoints));
                }
            }
            finally
            {
                Object.DestroyImmediate(baseInstance);
                Object.DestroyImmediate(variantInstance);
            }
        }

        [Test]
        public void NovaVariantBuildsAllInheritedTerrainCombinations()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/NovaVariants/Horizontal_Shrine.prefab").GetComponent<RoomTemplate>();
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var catalog = settings.Catalog;
            Assert.That(catalog.Templates, Does.Contain(variant));
            Assert.That(catalog.TryCreateSnapshot(out var snapshots, out var error), Is.True, error);
            var combinations = new HashSet<string>();
            for (int seed = 0; seed < 300 && combinations.Count < 4; seed++)
            {
                var plan = MapPlanner.Create(4, 4, seed, 32, snapshots);
                Assert.That(plan.Success, Is.True, plan.Error);
                if (!plan.Plan.Rooms.Any(room => room.TemplateId == variant.TemplateId)) continue;
                var build = MapBuilder.Build(plan.Plan, catalog, goal, visualGround: settings.VisualGround,
                    visualGroundMaterial: settings.VisualGroundMaterial);
                Assert.That(build.Success, Is.True, build.Error);
                try
                {
                    for (int i = 0; i < plan.Plan.Rooms.Count; i++)
                    {
                        var planned = plan.Plan.Rooms[i];
                        if (planned.TemplateId != variant.TemplateId) continue;
                        var room = build.Root.transform.GetChild(i).GetComponent<RoomTemplate>();
                        var terrain = planned.Terrain[0];
                        bool pattern = room.GetComponentsInChildren<Grid>(true).Length > 1;
                        bool filled = room.FixedGeometry.HasTile(new Vector3Int(3, 12, 0));
                        Assert.That(pattern, Is.EqualTo(!string.IsNullOrEmpty(terrain.PatternId)));
                        Assert.That(filled, Is.EqualTo(terrain.FilledCells.Count > 0));
                        Assert.That(room.transform.Find("BackgroundArt"), Is.Not.Null);
                        combinations.Add(pattern + ":" + filled);
                    }
                }
                finally { Object.DestroyImmediate(build.Root); }
            }
            Assert.That(combinations.Count, Is.EqualTo(4));

        }
    }
}
