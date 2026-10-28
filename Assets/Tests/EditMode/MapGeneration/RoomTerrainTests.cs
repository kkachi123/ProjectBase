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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Samples/Horizontal.prefab");
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
                t.VerifiedStartExits, t.VerifiedGoalEntries, verifiedDeadEndReturns: t.VerifiedDeadEndReturns, content: t.Content)).ToList();
            for (int seed = 0; seed < 100; seed++)
            {
                var a = MapPlanner.Create(4, 4, seed, 32, withTerrain);
                var b = MapPlanner.Create(4, 4, seed, 32, withoutTerrain);
                Assert.That(a.Success && b.Success, Is.True);
                Assert.That(a.Plan.Path, Is.EqualTo(b.Plan.Path));
                Assert.That(a.Plan.Rooms.Select(r => r.TemplateId), Is.EqualTo(b.Plan.Rooms.Select(r => r.TemplateId)));
            }
        }
    }
}
