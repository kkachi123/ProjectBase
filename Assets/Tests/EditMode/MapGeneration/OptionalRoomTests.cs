namespace ProjectRE.MapGeneration.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class OptionalRoomTests
    {
        private const string SettingsPath = "Assets/Prefabs/@Data/Map/MapPlanningSettings.asset";
        private const string GoalPath = "Assets/Prefabs/Map/GridChunk/@BackGroundPrefab/GoalTrigger/EndLight-EndTrigger.prefab";

        [Test]
        public void RealBranchesAreReciprocalAndDoNotChangeTheRequiredPath()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var withBranches, out var error), Is.True, error);
            var withoutBranches = withBranches.Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, t.PassagesVerified, t.HasPlayerStart, t.PlayerStart, t.HasGoal, t.Goal,
                t.VerifiedStartExits, t.VerifiedGoalEntries, t.Terrain, content: t.Content)).ToList();
            int count = 0;
            for (int seed = 0; seed < 1000; seed++)
            {
                var branched = MapPlanner.Create(4, 4, seed, 32, withBranches);
                var plain = MapPlanner.Create(4, 4, seed, 32, withoutBranches);
                Assert.That(branched.Success && plain.Success, Is.True, $"seed {seed}");
                Assert.That(branched.Plan.Path, Is.EqualTo(plain.Plan.Path));
                Assert.That(branched.Plan.PlayerStart, Is.EqualTo(plain.Plan.PlayerStart));
                Assert.That(branched.Plan.Goal, Is.EqualTo(plain.Plan.Goal));
                Assert.That(MapPlanValidator.Validate(branched.Plan, withBranches), Is.Empty);
                var optional = branched.Plan.Rooms.Where(r => r.IsOptional).ToArray();
                Assert.That(optional.Length, Is.LessThanOrEqualTo(1));
                if (optional.Length == 0) continue;
                count++;
                Assert.That(branched.Plan.Path.Contains(optional[0].Cell), Is.False);
                Assert.That(branched.Plan.Rooms.Count(r => r.Branch != RoomOpening.None), Is.EqualTo(1));
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        [Test]
        public void ContentWeightsDoNotAlterRouteBranchOrTerrain()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var original, out var error), Is.True, error);
            var changed = original.Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, t.PassagesVerified, t.HasPlayerStart, t.PlayerStart, t.HasGoal, t.Goal,
                t.VerifiedStartExits, t.VerifiedGoalEntries, t.Terrain, t.VerifiedDeadEndReturns,
                t.Content.Select(s => new ContentSlotSnapshot(s.SlotIndex, 99, s.Options)), t.BaseGroupId)).ToList();
            for (int seed = 0; seed < 100; seed++)
            {
                var a = MapPlanner.Create(4, 4, seed, 32, original);
                var b = MapPlanner.Create(4, 4, seed, 32, changed);
                Assert.That(a.Success && b.Success, Is.True);
                Assert.That(a.Plan.Path, Is.EqualTo(b.Plan.Path));
                Assert.That(a.Plan.Rooms.Select(r => (r.TemplateId, r.Branch, r.IsOptional)),
                    Is.EqualTo(b.Plan.Rooms.Select(r => (r.TemplateId, r.Branch, r.IsOptional))));
                for (int i = 0; i < a.Plan.Rooms.Count; i++)
                    Assert.That(a.Plan.Rooms[i].Terrain.Select(t => t.PatternId),
                        Is.EqualTo(b.Plan.Rooms[i].Terrain.Select(t => t.PatternId)));
            }
        }

        [TestCase(0, "item")]
        public void BuilderPlacesSelectedPrefabInOptionalRoom(int seed, string id)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var plan = settings.CreatePlan(seed);
            Assert.That(plan.Success, Is.True, plan.Error);
            var side = plan.Plan.Rooms.Single(r => r.IsOptional);
            Assert.That(side.Content.Single().OptionId, Is.EqualTo(id));
            var built = MapBuilder.Build(plan.Plan, settings.Catalog, goal);
            Assert.That(built.Success, Is.True, built.Error);
            try
            {
                int index = side.Cell.y * plan.Plan.Width + side.Cell.x;
                var instance = built.Root.transform.GetChild(index).GetComponent<RoomTemplate>();
                var slot = instance.ContentSettings.Single();
                var option = slot.options.Single(o => o.id == id);
                var matching = instance.GetComponentsInChildren<Collider2D>(true)
                    .Where(c => c.transform.parent == instance.transform && c.gameObject != instance.FixedGeometry.gameObject).ToArray();
                Assert.That(matching.Length, Is.EqualTo(1));
                Assert.That(matching[0].transform.localPosition,
                    Is.EqualTo(new Vector3(7.5f + option.offset.x, 4f + option.offset.y, 0)));
                Assert.That(matching[0].transform.localScale.x < 0, Is.EqualTo(slot.facing == ContentFacing.Left));
            }
            finally { Object.DestroyImmediate(built.Root); }
        }

        [Test]
        public void FinalRoomRoleControlsContentAndKeepsSeedReproducible()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var templates, out var error), Is.True, error);
            var byId = templates.ToDictionary(t => t.Id);
            var mainKinds = new HashSet<ContentKind>();
            int optionalItems = 0, optionalEmpty = 0, mainSlots = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var first = settings.CreatePlan(seed);
                var again = settings.CreatePlan(seed);
                Assert.That(first.Success && again.Success, Is.True, $"seed {seed}: {first.Error}");
                Assert.That(MapPlanValidator.Validate(first.Plan, templates), Is.Empty, $"seed {seed}");
                for (int i = 0; i < first.Plan.Rooms.Count; i++)
                {
                    var room = first.Plan.Rooms[i];
                    Assert.That(room.Content.Select(c => (c.SlotIndex, c.OptionId)),
                        Is.EqualTo(again.Plan.Rooms[i].Content.Select(c => (c.SlotIndex, c.OptionId))));
                    bool onPath = first.Plan.Path.Contains(room.Cell);
                    bool endpoint = room.Cell == first.Plan.Path[0] || room.Cell == first.Plan.Path[first.Plan.Path.Count - 1];
                    if ((!onPath && !room.IsOptional) || endpoint) Assert.That(room.Content, Is.Empty);
                    foreach (var selection in room.Content)
                    {
                        if (!room.IsOptional) mainSlots++;
                        if (selection.OptionId == "") { if (room.IsOptional) optionalEmpty++; continue; }
                        var kind = byId[room.TemplateId].Content.Single(s => s.SlotIndex == selection.SlotIndex)
                            .Options.Single(o => o.Id == selection.OptionId).Kind;
                        if (room.IsOptional)
                        {
                            Assert.That(kind, Is.EqualTo(ContentKind.Item).Or.EqualTo(ContentKind.EliteMonster));
                            if (kind == ContentKind.Item) optionalItems++;
                        }
                        else { Assert.That(kind, Is.EqualTo(ContentKind.Item).Or.EqualTo(ContentKind.Monster)); mainKinds.Add(kind); }
                    }
                }
            }
            Assert.That(mainSlots, Is.GreaterThan(0));
            Assert.That(mainKinds.Contains(ContentKind.Monster) && mainKinds.Contains(ContentKind.Item), Is.True);
            Assert.That(optionalItems, Is.GreaterThan(0));
            Assert.That(optionalEmpty, Is.GreaterThan(0));
        }

        [Test]
        public void RoleFilteringCannotSelectOtherRoleEvenWithNoEmptyWeight()
        {
            var slot = new ContentSlotSnapshot(0, 0, new[] { new ContentOptionSnapshot("monster", 1, ContentKind.Monster) });
            var room = new RoomTemplateSnapshot("role-test", RoomType.Horizontal, RoomOpening.East | RoomOpening.West,
                null, false, content: new[] { slot });
            Assert.That(RoomContentPlanner.Select(0, Vector2Int.zero, room, ContentRoomRole.Optional).Single().OptionId, Is.Empty);
            Assert.That(RoomContentPlanner.Select(0, Vector2Int.zero, room, ContentRoomRole.MainPath).Single().OptionId, Is.EqualTo("monster"));

            var eliteSlot = new ContentSlotSnapshot(0, 0, new[] { new ContentOptionSnapshot("elite", 1, ContentKind.EliteMonster) });
            var eliteRoom = new RoomTemplateSnapshot("elite-role-test", RoomType.Horizontal, RoomOpening.East | RoomOpening.West,
                null, false, content: new[] { eliteSlot });
            Assert.That(RoomContentPlanner.Select(0, Vector2Int.zero, eliteRoom, ContentRoomRole.MainPath).Single().OptionId, Is.Empty);
            Assert.That(RoomContentPlanner.Select(0, Vector2Int.zero, eliteRoom, ContentRoomRole.Optional).Single().OptionId, Is.EqualTo("elite"));
        }

        [TestCase("goblin")]
        [TestCase("item")]
        public void BuilderPlacesMainPathContentInInheritedSlot(string optionId)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            MapPlan selected = null;
            MapRoomPlan selectedRoom = null;
            for (int seed = 0; seed < 500 && selected == null; seed++)
            {
                var result = settings.CreatePlan(seed);
                Assert.That(result.Success, Is.True, result.Error);
                selectedRoom = result.Plan.Rooms.FirstOrDefault(r => !r.IsOptional &&
                    r.TemplateId == "production.down.west_forest_step" && r.Content.Any(c => c.OptionId == optionId));
                if (selectedRoom != null) selected = result.Plan;
            }
            Assert.That(selected, Is.Not.Null, $"No seed selected {optionId} in WestSouth");
            var build = MapBuilder.Build(selected, settings.Catalog, goal);
            Assert.That(build.Success, Is.True, build.Error);
            try
            {
                var room = build.Root.transform.GetChild(selectedRoom.Cell.y * selected.Width + selectedRoom.Cell.x)
                    .GetComponent<RoomTemplate>();
                var option = room.ContentSettings.Single().options.Single(o => o.id == optionId);
                var instance = room.transform.Find(option.prefab.name + "(Clone)");
                Assert.That(instance, Is.Not.Null);
                Assert.That(instance.localPosition, Is.EqualTo(new Vector3(7.5f + option.offset.x, 4f + option.offset.y, 0)));
            }
            finally { Object.DestroyImmediate(build.Root); }
        }

        [Test]
        public void ForgedBranchAndUnsafeSlotAreRejected()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var templates, out var error), Is.True, error);
            var plan = settings.CreatePlan(1).Plan;
            var rooms = plan.Rooms.ToList();
            int index = rooms.FindIndex(r => r.IsOptional);
            var old = rooms[index];
            rooms[index] = new MapRoomPlan(old.Cell, old.TemplateId, old.Type, old.Openings,
                old.Entry, RoomOpening.None, old.Terrain, isOptional: true, content: old.Content);
            var invalid = new MapPlan(plan.Width, plan.Height, plan.Seed, rooms, plan.Path, plan.PlayerStart, plan.Goal);
            Assert.That(MapPlanValidator.Validate(invalid, templates), Is.Not.Empty);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Base/CapWest.prefab");
            var room = Object.Instantiate(prefab).GetComponent<RoomTemplate>();
            try
            {
                var so = new SerializedObject(room);
                so.FindProperty("_contentSettings").GetArrayElementAtIndex(0).FindPropertyRelative("clearance").rectIntValue = new RectInt(7, 5, 1, 2);
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(RoomTemplateChecks.Validate(room), Is.Not.Empty);
            }
            finally { Object.DestroyImmediate(room.gameObject); }
        }

        [TestCase(ContentSurface.Floor, .5f, .3135f)]
        [TestCase(ContentSurface.Ceiling, .5f, 2.6775f)]
        [TestCase(ContentSurface.WestWall, -.2f, 1.2f)]
        [TestCase(ContentSurface.EastWall, 1.2f, 1.2f)]
        public void PlacementRequiresTheRequestedSolidSurface(ContentSurface surface, float xOffset, float yOffset)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Base/CapWest.prefab");
            var room = Object.Instantiate(prefab).GetComponent<RoomTemplate>();
            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>("Assets/Prefabs/Map/RoomTemplates/Shared/SampleSolid.asset");
            try
            {
                var so = new SerializedObject(room);
                so.FindProperty("_contentSlots").GetArrayElementAtIndex(0).vector2IntValue = new Vector2Int(10, 10);
                var slot = so.FindProperty("_contentSettings").GetArrayElementAtIndex(0);
                slot.FindPropertyRelative("clearance").rectIntValue = new RectInt(10, 10, 2, 3);
                slot.FindPropertyRelative("surface").enumValueIndex = (int)surface;
                var options = slot.FindPropertyRelative("options");
                options.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/Inventory/Item.prefab");
                options.GetArrayElementAtIndex(0).FindPropertyRelative("offset").vector2Value = new Vector2(xOffset, yOffset);
                options.arraySize = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                RectInt support = surface switch
                {
                    ContentSurface.Floor => new RectInt(10, 9, 2, 1),
                    ContentSurface.Ceiling => new RectInt(10, 13, 2, 1),
                    ContentSurface.WestWall => new RectInt(9, 10, 1, 3),
                    _ => new RectInt(12, 10, 1, 3),
                };
                foreach (Vector2Int cell in support.allPositionsWithin)
                    room.FixedGeometry.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
                Assert.That(RoomTemplateChecks.Validate(room), Is.Empty);
                room.FixedGeometry.SetTile(new Vector3Int(support.xMin, support.yMin, 0), null);
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("지지면")), Is.True);
            }
            finally { Object.DestroyImmediate(room.gameObject); }
        }
    }
}
