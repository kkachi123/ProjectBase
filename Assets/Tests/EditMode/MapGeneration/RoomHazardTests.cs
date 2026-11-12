namespace ProjectRE.MapGeneration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.Tilemaps;

    public sealed class RoomHazardTests
    {
        private const string SettingsPath = "Assets/Prefabs/@Data/Map/MapPlanningSettings.asset";
        private const string GoalPath = "Assets/Prefabs/Map/Goals/GoalTrigger/EndLight-EndTrigger.prefab";
        private const string BasePath = "Assets/Prefabs/Map/RoomTemplates/Base/NorthEast.prefab";

        [Test]
        public void WeightBoundariesAndSeedSelectionAreDeterministic()
        {
            RoomTemplateSnapshot Template(int spawn, int empty) => new("hazard-test", RoomType.Horizontal,
                RoomOpening.East | RoomOpening.West, null, false,
                hazards: new[] { new RoomHazardSnapshot("arrow", spawn, empty) });
            var always = Template(1, 0);
            var never = Template(0, 1);
            var mixed = Template(1, 1);
            var results = new HashSet<bool>();
            for (int seed = 0; seed < 100; seed++)
            {
                var cell = new Vector2Int(1, 2);
                Assert.That(RoomHazardPlanner.Select(seed, cell, always).Single().Spawn, Is.True);
                Assert.That(RoomHazardPlanner.Select(seed, cell, never).Single().Spawn, Is.False);
                bool selected = RoomHazardPlanner.Select(seed, cell, mixed).Single().Spawn;
                Assert.That(RoomHazardPlanner.Select(seed, cell, mixed).Single().Spawn, Is.EqualTo(selected));
                results.Add(selected);
            }
            Assert.That(results.Count, Is.EqualTo(2));
        }

        [Test]
        public void RealPlansKeepOtherChoicesAndBuilderAppliesBothHazardStates()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var originals, out var error), Is.True, error);
            var withoutHazards = originals.Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, t.PassagesVerified, t.HasPlayerStart, t.PlayerStart, t.HasGoal, t.Goal,
                t.VerifiedStartExits, t.VerifiedGoalEntries, t.Terrain, t.VerifiedDeadEndReturns,
                t.Content, t.BaseGroupId)).ToList();
            var selected = new Dictionary<bool, (MapPlan plan, MapRoomPlan room)>();
            for (int seed = 0; seed < 100; seed++)
            {
                var current = MapPlanner.Create(4, 4, seed, 32, originals);
                var baseline = MapPlanner.Create(4, 4, seed, 32, withoutHazards);
                Assert.That(current.Success && baseline.Success, Is.True, $"seed {seed}: {current.Error}");
                Assert.That(current.Plan.Path, Is.EqualTo(baseline.Plan.Path));
                for (int i = 0; i < current.Plan.Rooms.Count; i++)
                {
                    var room = current.Plan.Rooms[i];
                    var old = baseline.Plan.Rooms[i];
                    Assert.That((room.TemplateId, room.Branch, room.IsOptional), Is.EqualTo((old.TemplateId, old.Branch, old.IsOptional)));
                    Assert.That(room.Terrain.Select(t => (t.RegionIndex, t.PatternId, string.Join(",", t.FilledCells))),
                        Is.EqualTo(old.Terrain.Select(t => (t.RegionIndex, t.PatternId, string.Join(",", t.FilledCells)))));
                    Assert.That(room.Content.Select(c => (c.SlotIndex, c.OptionId)), Is.EqualTo(old.Content.Select(c => (c.SlotIndex, c.OptionId))));
                    foreach (var hazard in room.Hazards) if (!selected.ContainsKey(hazard.Spawn)) selected.Add(hazard.Spawn, (current.Plan, room));
                }
            }
            Assert.That(selected.Keys, Is.EquivalentTo(new[] { true, false }));
            foreach (var pair in selected)
            {
                var built = MapBuilder.Build(pair.Value.plan, settings.Catalog, goal);
                Assert.That(built.Success, Is.True, built.Error);
                try
                {
                    var cell = pair.Value.room.Cell;
                    var room = built.Root.transform.GetChild(cell.y * pair.Value.plan.Width + cell.x).GetComponent<RoomTemplate>();
                    foreach (var selection in pair.Value.room.Hazards)
                    {
                        var slot = room.Hazards.Single(h => h.id == selection.Id);
                        Assert.That(slot.candidate.activeSelf, Is.EqualTo(selection.Spawn), selection.Id);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(built.Root); }
            }
        }

        [Test]
        public void ForgedHazardSelectionAndUnsafeAuthoringAreRejected()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            Assert.That(settings.Catalog.TryCreateSnapshot(out var snapshots, out var error), Is.True, error);
            var plan = settings.CreatePlan(0).Plan;
            var rooms = plan.Rooms.ToList();
            int index = rooms.FindIndex(r => r.Hazards.Count > 0);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            var old = rooms[index];
            var wrong = old.Hazards.Select(h => new RoomHazardSelection(h.Id, !h.Spawn));
            rooms[index] = new MapRoomPlan(old.Cell, old.TemplateId, old.Type, old.Openings, old.Entry, old.Exit,
                old.Terrain, old.Branch, old.IsOptional, old.Content, wrong);
            var forged = new MapPlan(plan.Width, plan.Height, plan.Seed, rooms, plan.Path, plan.PlayerStart, plan.Goal);
            Assert.That(MapPlanValidator.Validate(forged, snapshots).Any(e => e.Contains("함정 선택")), Is.True);

            var room = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath)).GetComponent<RoomTemplate>();
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/Map/RoomTemplates/Shared/SampleSolid.asset");
            try
            {
                Assert.That(RoomTemplateChecks.Validate(room), Is.Empty);
                room.FixedGeometry.SetTile(new Vector3Int(5, 5, 0), tile);
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("함정 공간")), Is.True);
                room.FixedGeometry.SetTile(new Vector3Int(5, 5, 0), null);
                room.FixedGeometry.SetTile(new Vector3Int(0, 5, 0), null);
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("함정 지지면")), Is.True);
                room.FixedGeometry.SetTile(new Vector3Int(0, 5, 0), tile);
                room.Hazards.Single().candidate.SetActive(true);
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("함정 후보")), Is.True);
                room.Hazards.Single().candidate.SetActive(false);
                room.Hazards.Single().passageVerified = false;
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("통과 검증")), Is.True);
                room.Hazards.Single().passageVerified = true;
                room.Hazards.Single().spawnWeight = 0;
                room.Hazards.Single().emptyWeight = 0;
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("함정 가중치")), Is.True);
                room.Hazards.Single().spawnWeight = 1;
                room.Hazards.Single().emptyWeight = 1;
                room.Hazards.Single().clearance = new RectInt(1, 4, 16, 7);
                Assert.That(RoomTemplateChecks.Validate(room).Any(e => e.Contains("함정이 변형 영역")), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(room.gameObject); }
        }

        [Test]
        public void PreviewUndoRedoPreservesSelectedHazardState()
        {
            Scene scene = SceneManager.GetActiveScene();
            int initialRoots = scene.rootCount;
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            MapRoomPlan selected = null;
            string hazardId = null;
            int selectedSeed = -1;
            for (int seed = 0; seed < 1000 && selected == null; seed++)
            {
                var plan = settings.CreatePlan(seed);
                Assert.That(plan.Success, Is.True, plan.Error);
                selected = plan.Plan.Rooms.FirstOrDefault(r => r.Hazards.Any(h => h.Spawn));
                if (selected != null) { selectedSeed = seed; hazardId = selected.Hazards.First(h => h.Spawn).Id; }
            }
            Assert.That(selected, Is.Not.Null);
            EditorWindow window = null;
            try
            {
                Type type = Type.GetType("ProjectRE.MapGeneration.Editor.RoomMapGeneratorWindow, Assembly-CSharp-Editor");
                window = (EditorWindow)ScriptableObject.CreateInstance(type);
                type.GetMethod("CreateGUI").Invoke(window, null);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                type.GetField("_settings", flags).SetValue(window, settings);
                type.GetField("_seed", flags).SetValue(window, selectedSeed);
                var owner = type.GetField("_previewRoot", flags);
                type.GetMethod("Generate", flags).Invoke(window, null);
                bool Active() => ((GameObject)owner.GetValue(window)).GetComponentsInChildren<RoomTemplate>(true)
                    .Single(r => r.transform.localPosition == new Vector3(selected.Cell.x * 20, -selected.Cell.y * 20, 0))
                    .Hazards.Single(h => h.id == hazardId).candidate.activeSelf;
                Assert.That(Active(), Is.True);
                Undo.PerformUndo();
                Assert.That(scene.rootCount, Is.EqualTo(initialRoots));
                Undo.PerformRedo();
                Assert.That(Active(), Is.True);
            }
            finally
            {
                if (window != null)
                {
                    var owned = (System.Collections.IEnumerable)window.GetType().GetField("_ownedRoots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                    foreach (GameObject root in owned) if (root != null) UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }
    }
}
