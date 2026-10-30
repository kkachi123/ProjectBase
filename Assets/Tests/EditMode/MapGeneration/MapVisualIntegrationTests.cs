namespace ProjectRE.MapGeneration.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public sealed class MapVisualIntegrationTests
    {
        private const string SettingsPath = "Assets/Prefabs/@Data/Map/MapPlanningSettings.asset";
        private const string GoalPath = "Assets/Prefabs/Map/GridChunk/@BackGroundPrefab/GoalTrigger/EndLight-EndTrigger.prefab";

        [TestCase(264, "item")]
        [TestCase(151, "")]
        [TestCase(337, "item")]
        public void RuinedCourtBuildsOptionalBranchWithSelectedContent(int seed, string optionId)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, seed, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                MapRoomPlan planned = null;
                foreach (var candidate in result.Plan.Rooms)
                    if (candidate.IsOptional && candidate.TemplateId == "production.optional.ruined_court_west") planned = candidate;
                Assert.That(planned, Is.Not.Null);
                Assert.That(planned.Entry, Is.EqualTo(RoomOpening.West));
                Assert.That(planned.Content.Count, Is.EqualTo(1));
                Assert.That(planned.Content[0].OptionId, Is.EqualTo(optionId));
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == planned.TemplateId);
                Assert.That(room, Is.Not.Null);
                Assert.That(room.VerifiedDeadEndReturns, Is.EqualTo(RoomOpening.West));
                Assert.That(room.OptionalTarget, Is.Not.Null);
                Assert.That(room.PlayerStart, Is.Null);
                Assert.That(room.Goal, Is.Null);
                var west = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(-RoomTemplate.RoomSize, 0, 0));
                Assert.That(west, Is.Not.Null);
                Assert.That(west.Openings.HasFlag(RoomOpening.East), Is.True);
                var slot = room.ContentSettings[0];
                foreach (var option in slot.options)
                {
                    var instance = room.transform.Find(option.prefab.name + "(Clone)");
                    if (option.id != optionId) Assert.That(instance, Is.Null);
                    else
                    {
                        Assert.That(instance, Is.Not.Null);
                        Assert.That(instance.localPosition, Is.EqualTo(new Vector3(7.5f + option.offset.x, 4f + option.offset.y, 0)));
                    }
                }
                Assert.That(room.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void ForestDropBuildsAsStartWithVerifiedSouthernExit()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 10, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.start.forest_drop_south");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.PlayerStart, Is.Not.Null);
                Assert.That(result.Plan.PlayerStart, Is.EqualTo(room.transform.localPosition + room.PlayerStart.localPosition));
                Assert.That(room.VerifiedStartExits, Is.EqualTo(RoomOpening.South));
                Assert.That(room.Goal, Is.Null);
                Assert.That(room.OptionalTarget, Is.Null);
                for (int x = 8; x <= 10; x++)
                    Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(x, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                for (int x = 13; x <= 16; x++)
                    Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 0, 0)), Is.False);
                var below = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(0, -RoomTemplate.RoomSize, 0));
                Assert.That(below, Is.Not.Null);
                Assert.That(below.Openings.HasFlag(RoomOpening.North), Is.True);
                Assert.That(room.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void ShrineWestBuildsAsStartWithVerifiedWesternExit()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 4, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.start.shrine_west");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.PlayerStart, Is.Not.Null);
                Assert.That(result.Plan.PlayerStart, Is.EqualTo(room.transform.localPosition + room.PlayerStart.localPosition));
                Assert.That(room.VerifiedStartExits, Is.EqualTo(RoomOpening.West));
                Assert.That(room.Goal, Is.Null);
                Assert.That(room.OptionalTarget, Is.Null);
                for (int x = 8; x <= 10; x++)
                    Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(x, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                var west = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(-RoomTemplate.RoomSize, 0, 0));
                Assert.That(west, Is.Not.Null);
                Assert.That(west.Openings.HasFlag(RoomOpening.East), Is.True);
                var art = room.transform.Find("BackgroundArt");
                Assert.That(art.Find("Shrine Statue"), Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void OvergrownCourtBuildsAsGoalWithVerifiedEasternEntry()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 0, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.goal.overgrown_court_east");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.Goal, Is.Not.Null);
                Assert.That(room.VerifiedGoalEntries, Is.EqualTo(RoomOpening.East));
                Assert.That(room.PlayerStart, Is.Null);
                Assert.That(room.OptionalTarget, Is.Null);
                Assert.That(result.Plan.Goal, Is.EqualTo(room.transform.localPosition + room.Goal.localPosition));
                for (int x = 13; x <= 15; x++)
                    Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(x, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                var east = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(RoomTemplate.RoomSize, 0, 0));
                Assert.That(east, Is.Not.Null);
                Assert.That(east.Openings.HasFlag(RoomOpening.West), Is.True);
                var trigger = result.Root.transform.Find(goal.name + "(Clone)");
                Assert.That(trigger, Is.Not.Null);
                Assert.That(trigger.localPosition, Is.EqualTo(result.Plan.Goal));
                Assert.That(trigger.GetComponent<Collider2D>().isTrigger, Is.True);
                Assert.That(room.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void ForestDescentBuildsAsGoalWithVerifiedNorthernEntry()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 4, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.goal.forest_descent_north");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.Goal, Is.Not.Null);
                Assert.That(room.VerifiedGoalEntries, Is.EqualTo(RoomOpening.North));
                Assert.That(room.PlayerStart, Is.Null);
                Assert.That(room.OptionalTarget, Is.Null);
                Assert.That(result.Plan.Goal, Is.EqualTo(room.transform.localPosition + room.Goal.localPosition));
                for (int x = 13; x <= 16; x++)
                {
                    Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 19, 0)), Is.False);
                    Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(x, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                }
                var above = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(0, RoomTemplate.RoomSize, 0));
                Assert.That(above, Is.Not.Null);
                Assert.That(above.Openings.HasFlag(RoomOpening.South), Is.True);
                var trigger = result.Root.transform.Find(goal.name + "(Clone)");
                Assert.That(trigger, Is.Not.Null);
                Assert.That(trigger.localPosition, Is.EqualTo(result.Plan.Goal));
                Assert.That(trigger.GetComponent<Collider2D>().isTrigger, Is.True);
                Assert.That(room.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void RuinedSanctuaryBuildsAsGoalWithVerifiedWesternEntry()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 9, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.goal.ruined_sanctuary_west");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.Goal, Is.Not.Null);
                Assert.That(room.VerifiedGoalEntries, Is.EqualTo(RoomOpening.West));
                Assert.That(room.PlayerStart, Is.Null);
                Assert.That(room.OptionalTarget, Is.Null);
                Assert.That(result.Plan.Goal, Is.EqualTo(room.transform.localPosition + room.Goal.localPosition));
                var west = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(-RoomTemplate.RoomSize, 0, 0));
                Assert.That(west, Is.Not.Null);
                Assert.That(west.Openings.HasFlag(RoomOpening.East), Is.True);
                var trigger = result.Root.transform.Find(goal.name + "(Clone)");
                Assert.That(trigger, Is.Not.Null);
                Assert.That(trigger.localPosition, Is.EqualTo(result.Plan.Goal));
                Assert.That(trigger.GetComponent<Collider2D>().isTrigger, Is.True);
                var art = room.transform.Find("BackgroundArt");
                Assert.That(art.Find("Goal Pedestal"), Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void ForestGatewayBuildsAsStartWithVerifiedEasternExit()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 1, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.start.forest_gateway_east");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.PlayerStart, Is.Not.Null);
                Assert.That(result.Plan.PlayerStart, Is.EqualTo(room.transform.localPosition + room.PlayerStart.localPosition));
                Assert.That(room.VerifiedStartExits, Is.EqualTo(RoomOpening.East));
                Assert.That(room.Goal, Is.Null);
                Assert.That(room.OptionalTarget, Is.Null);
                Assert.That(room.ContentSettings, Is.Empty);
                Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(12, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                var east = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(RoomTemplate.RoomSize, 0, 0));
                Assert.That(east, Is.Not.Null);
                Assert.That(east.Openings.HasFlag(RoomOpening.West), Is.True);
                var art = room.transform.Find("BackgroundArt");
                Assert.That(art.Find("Left Arch"), Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void RuinedShaftBuildsAsConnectedConsecutiveDescent()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 1, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var upper = System.Array.Find(rooms, room => room.TemplateId == "production.downthrough.ruined_shaft" &&
                    room.transform.localPosition == new Vector3(40, -20, 0));
                var lower = System.Array.Find(rooms, room => room.TemplateId == "production.downthrough.ruined_shaft" &&
                    room.transform.localPosition == new Vector3(40, -40, 0));
                Assert.That(upper, Is.Not.Null);
                Assert.That(lower, Is.Not.Null);
                foreach (var room in new[] { upper, lower })
                {
                    Assert.That(room.PassagesVerified, Is.True);
                    Assert.That(room.Passages.Count, Is.EqualTo(1));
                    Assert.That(room.Passages[0].entry, Is.EqualTo(RoomOpening.North));
                    Assert.That(room.Passages[0].exit, Is.EqualTo(RoomOpening.South));
                    for (int x = 13; x <= 16; x++)
                    {
                        Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 19, 0)), Is.False);
                        Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 0, 0)), Is.False);
                        Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 12, 0)), Is.False);
                    }
                    Assert.That(room.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
                }
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void ShrineAtriumBuildsWithNorthAndEastConnections()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 0, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.arrival.northeast_shrine_atrium");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.PassagesVerified, Is.True);
                Assert.That(room.Passages.Count, Is.EqualTo(1));
                Assert.That(room.Passages[0].entry, Is.EqualTo(RoomOpening.North));
                Assert.That(room.Passages[0].exit, Is.EqualTo(RoomOpening.East));
                for (int x = 13; x <= 16; x++)
                {
                    Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 19, 0)), Is.False);
                    Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(x, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                }
                var above = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(0, RoomTemplate.RoomSize, 0));
                var east = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(RoomTemplate.RoomSize, 0, 0));
                Assert.That(above, Is.Not.Null);
                Assert.That(east, Is.Not.Null);
                Assert.That(above.Openings.HasFlag(RoomOpening.South), Is.True);
                Assert.That(east.Openings.HasFlag(RoomOpening.West), Is.True);
                var art = room.transform.Find("BackgroundArt");
                Assert.That(art.Find("Shrine Statue"), Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void PrototypeCatalogFixedTerrainUsesSupportedGridColliders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            foreach (var room in settings.Catalog.Templates)
            foreach (Vector3Int cell in room.FixedGeometry.cellBounds.allPositionsWithin)
                if (room.FixedGeometry.HasTile(cell))
                    Assert.That(room.FixedGeometry.GetColliderType(cell), Is.EqualTo(Tile.ColliderType.Grid),
                        $"Unsupported terrain in {room.TemplateId} at {cell}");
        }

        [Test]
        public void RuinedOverlookBuildsWithEastAndSouthConnections()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var result = MapGenerator.Generate(settings, 3, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var rooms = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.down.east_ruined_overlook");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.PassagesVerified, Is.True);
                Assert.That(room.Passages.Count, Is.EqualTo(1));
                Assert.That(room.Passages[0].entry, Is.EqualTo(RoomOpening.East));
                Assert.That(room.Passages[0].exit, Is.EqualTo(RoomOpening.South));
                Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(17, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                for (int x = 13; x <= 16; x++)
                    Assert.That(room.FixedGeometry.HasTile(new Vector3Int(x, 0, 0)), Is.False);
                var east = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(RoomTemplate.RoomSize, 0, 0));
                var below = System.Array.Find(rooms, candidate => candidate.transform.localPosition ==
                    room.transform.localPosition + new Vector3(0, -RoomTemplate.RoomSize, 0));
                Assert.That(east, Is.Not.Null);
                Assert.That(below, Is.Not.Null);
                Assert.That(east.Openings.HasFlag(RoomOpening.West), Is.True);
                Assert.That(below.Openings.HasFlag(RoomOpening.North), Is.True);
                var art = room.transform.Find("BackgroundArt");
                Assert.That(art.Find("Overlook Rail 1"), Is.Not.Null);
                Assert.That(art.Find("Broken Pillar 1"), Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void GeneratorAppliesConfiguredTerrainMaterialOnlyToVisualTilemap()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            Assert.That(settings.VisualGroundMaterial, Is.Not.Null);
            var result = MapGenerator.Generate(settings, 0, goal);
            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                var visual = result.Root.transform.Find("Terrain Visual Grid/Terrain Visual");
                Assert.That(visual.GetComponent<TilemapRenderer>().sharedMaterial,
                    Is.SameAs(settings.VisualGroundMaterial));
                Assert.That(visual.GetComponent<TilemapCollider2D>(), Is.Null);
                var room = System.Array.Find(result.Root.GetComponentsInChildren<RoomTemplate>(true),
                    candidate => candidate.TemplateId == "production.down.west_forest_step");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(9, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                Assert.That(room.FixedGeometry.GetComponent<TilemapRenderer>().sharedMaterial,
                    Is.Not.SameAs(settings.VisualGroundMaterial));
            }
            finally { if (result.Root != null) Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void RuinedGalleryBuildsBetweenDescentAndWesternRoom()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var plan = settings.CreatePlan(0);
            Assert.That(plan.Success, Is.True, plan.Error);
            var build = MapBuilder.Build(plan.Plan, settings.Catalog, AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath),
                visualGround: settings.VisualGround);
            try
            {
                Assert.That(build.Success, Is.True, build.Error);
                var rooms = build.Root.GetComponentsInChildren<RoomTemplate>(true);
                var arrival = System.Array.Find(rooms, room => room.TemplateId == "production.arrival.northwest_gallery");
                Assert.That(arrival, Is.Not.Null);
                Assert.That(arrival.PassagesVerified, Is.True);
                Assert.That(arrival.Passages.Count, Is.EqualTo(1));
                Assert.That(arrival.Passages[0].entry, Is.EqualTo(RoomOpening.North));
                Assert.That(arrival.Passages[0].exit, Is.EqualTo(RoomOpening.West));
                for (int x = 13; x <= 16; x++)
                {
                    Assert.That(arrival.FixedGeometry.HasTile(new Vector3Int(x, 19, 0)), Is.False);
                    Assert.That(arrival.FixedGeometry.GetColliderType(new Vector3Int(x, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                }
                var above = System.Array.Find(rooms, room => room.transform.localPosition ==
                    arrival.transform.localPosition + new Vector3(0, RoomTemplate.RoomSize, 0));
                var west = System.Array.Find(rooms, room => room.transform.localPosition ==
                    arrival.transform.localPosition + new Vector3(-RoomTemplate.RoomSize, 0, 0));
                Assert.That(above, Is.Not.Null);
                Assert.That(west, Is.Not.Null);
                Assert.That((above.Openings & RoomOpening.South) != 0, Is.True);
                Assert.That((west.Openings & RoomOpening.East) != 0, Is.True);
                Assert.That(arrival.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (build.Root != null) Object.DestroyImmediate(build.Root); }
        }

        [Test]
        public void ForestDescentBuildsWithVerifiedWestToSouthPassageAndOpenLowerNeighbor()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var result = settings.CreatePlan(0);
            Assert.That(result.Success, Is.True, result.Error);
            var build = MapBuilder.Build(result.Plan, settings.Catalog, AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath),
                visualGround: settings.VisualGround);
            try
            {
                Assert.That(build.Success, Is.True, build.Error);
                var rooms = build.Root.GetComponentsInChildren<RoomTemplate>(true);
                var down = System.Array.Find(rooms, room => room.TemplateId == "production.down.west_forest_step");
                Assert.That(down, Is.Not.Null);
                Assert.That(down.PassagesVerified, Is.True);
                Assert.That(down.Passages.Count, Is.EqualTo(1));
                Assert.That(down.Passages[0].entry, Is.EqualTo(RoomOpening.West));
                Assert.That(down.Passages[0].exit, Is.EqualTo(RoomOpening.South));
                Assert.That(down.FixedGeometry.GetColliderType(new Vector3Int(9, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                for (int x = 13; x <= 16; x++)
                    Assert.That(down.FixedGeometry.HasTile(new Vector3Int(x, 0, 0)), Is.False);
                var below = System.Array.Find(rooms, room => room.transform.localPosition ==
                    down.transform.localPosition + new Vector3(0, -RoomTemplate.RoomSize, 0));
                Assert.That(below, Is.Not.Null);
                Assert.That((below.Openings & RoomOpening.North) != 0, Is.True);
                Assert.That(down.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (build.Root != null) Object.DestroyImmediate(build.Root); }
        }

        [Test]
        public void ShrineRoomSharesBaseGeometryAndBuildsAlongsideForest()
        {
            var forest = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Map/RoomTemplates/NovaVariants/Horizontal_Forest.prefab").GetComponent<RoomTemplate>();
            var shrine = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Map/RoomTemplates/NovaVariants/Horizontal_Shrine.prefab").GetComponent<RoomTemplate>();
            Assert.That(shrine.PassagesVerified, Is.True);
            Assert.That(shrine.Openings, Is.EqualTo(forest.Openings));
            Assert.That(shrine.Passages.Count, Is.EqualTo(forest.Passages.Count));
            for (int i = 0; i < forest.Passages.Count; i++)
            {
                Assert.That(shrine.Passages[i].entry, Is.EqualTo(forest.Passages[i].entry));
                Assert.That(shrine.Passages[i].exit, Is.EqualTo(forest.Passages[i].exit));
            }
            var first = forest.FixedGeometry;
            var second = shrine.FixedGeometry;
            Assert.That(second.gameObject.layer, Is.EqualTo(first.gameObject.layer));
            Assert.That(second.transform.localPosition, Is.EqualTo(first.transform.localPosition));
            Assert.That(second.transform.localRotation, Is.EqualTo(first.transform.localRotation));
            Assert.That(second.transform.localScale, Is.EqualTo(first.transform.localScale));
            var firstCollider = first.GetComponent<TilemapCollider2D>();
            var secondCollider = second.GetComponent<TilemapCollider2D>();
            Assert.That(secondCollider.enabled, Is.EqualTo(firstCollider.enabled));
            Assert.That(secondCollider.isTrigger, Is.EqualTo(firstCollider.isTrigger));
            Assert.That(secondCollider.offset, Is.EqualTo(firstCollider.offset));
            Assert.That(secondCollider.usedByComposite, Is.EqualTo(firstCollider.usedByComposite));
            for (int x = 0; x < RoomTemplate.RoomSize; x++)
            for (int y = 0; y < RoomTemplate.RoomSize; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                Assert.That(second.GetTile(cell), Is.SameAs(first.GetTile(cell)), $"Tile at {cell}");
                Assert.That(second.GetColliderType(cell), Is.EqualTo(first.GetColliderType(cell)), $"Collider at {cell}");
            }
            Assert.That(shrine.transform.Find("BackgroundArt").GetComponentsInChildren<Collider2D>(true), Is.Empty);

            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var plan = settings.CreatePlan(8);
            Assert.That(plan.Success, Is.True, plan.Error);
            var build = MapBuilder.Build(plan.Plan, settings.Catalog, AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath),
                visualGround: settings.VisualGround);
            try
            {
                Assert.That(build.Success, Is.True, build.Error);
                var rooms = build.Root.GetComponentsInChildren<RoomTemplate>(true);
                var builtShrine = System.Array.Find(rooms, room => room.TemplateId == shrine.TemplateId);
                var builtForest = System.Array.Find(rooms, room => room.TemplateId == forest.TemplateId);
                Assert.That(builtShrine, Is.Not.Null);
                Assert.That(builtForest, Is.Not.Null);
                Assert.That(builtShrine.transform.localPosition - builtForest.transform.localPosition,
                    Is.EqualTo(new Vector3(RoomTemplate.RoomSize, 0, 0)));
                Assert.That(builtShrine.transform.Find("BackgroundArt/Shrine Statue"), Is.Not.Null);
            }
            finally { if (build.Root != null) Object.DestroyImmediate(build.Root); }
        }

        [Test]
        public void NovaCatalogBuildsDecoratedRoomWithItsVerifiedTerrain()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            Assert.That(settings, Is.Not.Null);
            var plan = settings.CreatePlan(0);
            Assert.That(plan.Success, Is.True, plan.Error);
            var build = MapBuilder.Build(plan.Plan, settings.Catalog, goal, visualGround: settings.VisualGround);
            try
            {
                Assert.That(build.Success, Is.True, build.Error);
                var rooms = build.Root.GetComponentsInChildren<RoomTemplate>(true);
                var room = System.Array.Find(rooms, candidate => candidate.TemplateId == "production.horizontal.step");
                Assert.That(room, Is.Not.Null);
                Assert.That(room.PassagesVerified, Is.True);
                Assert.That(room.FixedGeometry.GetColliderType(new Vector3Int(10, 3, 0)), Is.EqualTo(Tile.ColliderType.Grid));
                var art = room.transform.Find("BackgroundArt");
                Assert.That(art, Is.Not.Null);
                Assert.That(art.Find("Canopy Near"), Is.Not.Null);
                Assert.That(art.Find("Trunk 1"), Is.Not.Null);
                Assert.That(art.Find("Underbrush 1"), Is.Not.Null);
                foreach (var renderer in art.GetComponentsInChildren<SpriteRenderer>(true))
                    Assert.That(renderer.sprite, Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally { if (build.Root != null) Object.DestroyImmediate(build.Root); }
        }

        [Test]
        public void TerrainTileSuppliesSpritesForAllRelevantNeighborShapesWithoutColliders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null);
            var tile = settings.VisualGround;
            Assert.That(tile, Is.Not.Null);
            var grid = new GameObject("Rule check grid", typeof(Grid));
            try
            {
                var mapObject = new GameObject("Rule check tilemap", typeof(Tilemap), typeof(TilemapRenderer));
                mapObject.transform.SetParent(grid.transform, false);
                var map = mapObject.GetComponent<Tilemap>();
                var cardinal = new[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
                var diagonal = new[] {
                    new Vector3Int(-1, 1, 0), new Vector3Int(1, 1, 0),
                    new Vector3Int(-1, -1, 0), new Vector3Int(1, -1, 0)
                };
                var adjacent = new[] { 9, 3, 12, 6 };
                var singleCorners = new[] { "Corner_NW", "Corner_NE", "Corner_SW", "Corner_SE" };
                int count = 0;
                for (int cardinalMask = 0; cardinalMask < 16; cardinalMask++)
                {
                    int relevant = 0;
                    for (int i = 0; i < 4; i++)
                        if ((cardinalMask & adjacent[i]) == adjacent[i]) relevant |= 1 << i;
                    for (int missing = 0; missing < 16; missing++)
                    {
                        if ((missing & ~relevant) != 0) continue;
                        map.ClearAllTiles();
                        map.SetTile(Vector3Int.zero, tile);
                        for (int i = 0; i < 4; i++)
                        {
                            if ((cardinalMask & (1 << i)) != 0) map.SetTile(cardinal[i], tile);
                            if ((missing & (1 << i)) == 0) map.SetTile(diagonal[i], tile);
                        }
                        map.RefreshAllTiles();
                        var sprite = map.GetSprite(Vector3Int.zero);
                        Assert.That(sprite, Is.Not.Null, $"Missing Sprite: {cardinalMask}, {missing}");
                        Assert.That(map.GetColliderType(Vector3Int.zero), Is.EqualTo(Tile.ColliderType.None));
                        Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(.001f));
                        Assert.That(sprite.bounds.size.y, Is.EqualTo(1f).Within(.001f));
                        if (missing != 0)
                        {
                            string expected = $"M{cardinalMask:D2}_D{missing:D2}";
                            for (int i = 0; i < 4; i++)
                                if (cardinalMask == 15 && missing == (1 << i))
                                    expected = singleCorners[i];
                            Assert.That(sprite.name, Is.EqualTo(expected), $"Wrong shape: {cardinalMask}, {missing}");
                        }
                        count++;
                    }
                }
                Assert.That(count, Is.EqualTo(47));
            }
            finally { Object.DestroyImmediate(grid); }
        }

        [TestCase(42)]
        [TestCase(2026)]
        public void VisualCellsMatchGeneratedTerrainWithoutChangingColliders(int seed)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.VisualGround, Is.Not.Null);
            var plan = settings.CreatePlan(seed);
            Assert.That(plan.Success, Is.True, plan.Error);

            var plain = MapBuilder.Build(plan.Plan, settings.Catalog, goal);
            var decorated = MapBuilder.Build(plan.Plan, settings.Catalog, goal, visualGround: settings.VisualGround);
            try
            {
                Assert.That(plain.Success, Is.True, plain.Error);
                Assert.That(decorated.Success, Is.True, decorated.Error);
                var visual = decorated.Root.transform.Find("Terrain Visual Grid/Terrain Visual").GetComponent<Tilemap>();
                Assert.That(visual.GetComponent<TilemapCollider2D>(), Is.Null);

                var expected = new HashSet<Vector3Int>();
                foreach (var source in decorated.Root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (source == visual || source.GetComponent<TilemapRenderer>().enabled) continue;
                    foreach (Vector3Int local in source.cellBounds.allPositionsWithin)
                    {
                        if (!source.HasTile(local)) continue;
                        Assert.That(source.GetColliderType(local), Is.EqualTo(Tile.ColliderType.Grid));
                        // The builder deliberately leaves the root inactive; GridLayout's
                        // CellToWorld is not initialized until activation.
                        var world = source.transform.TransformPoint((Vector3)local);
                        var cell = Vector3Int.RoundToInt(visual.transform.InverseTransformPoint(world));
                        Assert.That(expected.Add(cell), Is.True, $"Duplicate visual cell {cell}");
                        Assert.That(visual.GetTile(cell), Is.SameAs(settings.VisualGround));
                    }
                }

                int visualCount = 0;
                foreach (Vector3Int cell in visual.cellBounds.allPositionsWithin)
                    if (visual.HasTile(cell)) { visualCount++; Assert.That(expected.Contains(cell), Is.True, $"Unexpected visual cell {cell}"); }
                Assert.That(visualCount, Is.EqualTo(expected.Count));

                var plainRooms = plain.Root.GetComponentsInChildren<RoomTemplate>(true);
                var visualRooms = decorated.Root.GetComponentsInChildren<RoomTemplate>(true);
                Assert.That(visualRooms.Length, Is.EqualTo(plainRooms.Length));
                for (int i = 0; i < plainRooms.Length; i++)
                {
                    Assert.That(visualRooms[i].TemplateId, Is.EqualTo(plainRooms[i].TemplateId));
                    Assert.That(visualRooms[i].transform.position, Is.EqualTo(plainRooms[i].transform.position));
                    var before = plainRooms[i].FixedGeometry;
                    var after = visualRooms[i].FixedGeometry;
                    foreach (Vector3Int cell in before.cellBounds.allPositionsWithin)
                    {
                        Assert.That(after.GetTile(cell), Is.SameAs(before.GetTile(cell)));
                        if (before.HasTile(cell)) Assert.That(after.GetColliderType(cell), Is.EqualTo(before.GetColliderType(cell)));
                    }
                }
            }
            finally
            {
                if (plain.Root != null) Object.DestroyImmediate(plain.Root);
                if (decorated.Root != null) Object.DestroyImmediate(decorated.Root);
            }
        }
    }
}
