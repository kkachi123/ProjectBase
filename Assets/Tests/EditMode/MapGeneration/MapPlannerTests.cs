namespace ProjectRE.MapGeneration.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class MapPlannerTests
    {
        private static readonly RoomOpening[] Sides =
            { RoomOpening.North, RoomOpening.East, RoomOpening.South, RoomOpening.West };

        [Test]
        public void ThousandSeedsProduceValidPlans()
        {
            var catalog = CompleteCatalog();
            for (int seed = 0; seed < 1000; seed++)
            {
                MapPlanningResult result = MapPlanner.Create(4, 4, seed, 32, catalog);
                Assert.That(result.Success, Is.True, $"seed {seed}: {result.Error}");
                Assert.That(MapPlanValidator.Validate(result.Plan, catalog), Is.Empty, $"seed {seed}");
                Assert.That(result.Plan.Rooms.Count, Is.EqualTo(16));
                Assert.That(result.Plan.Path[0].y, Is.Zero);
                Assert.That(result.Plan.Path[result.Plan.Path.Count - 1].y, Is.EqualTo(3));
            }
        }

        [Test]
        public void RealCatalogProducesValidPlansForThousandSeeds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomTemplateCatalog>("Assets/Prefabs/@Data/Map/RoomTemplateCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryCreateSnapshot(out var templates, out var error), Is.True, error);
            for (int seed = 0; seed < 1000; seed++)
            {
                var result = MapPlanner.Create(4, 4, seed, 32, templates);
                Assert.That(result.Success, Is.True, $"seed {seed}: {result.Error}");
                Assert.That(MapPlanValidator.Validate(result.Plan, templates), Is.Empty, $"seed {seed}");
            }
        }

        [Test]
        public void SameSeedAndCatalogProduceSamePlanRegardlessOfInputOrder()
        {
            var templates = CompleteCatalog();
            var first = MapPlanner.Create(4, 4, -1729, 32, templates);
            templates.Reverse();
            var second = MapPlanner.Create(4, 4, -1729, 32, templates);
            Assert.That(first.Success && second.Success, Is.True);
            Assert.That(first.Plan.Path, Is.EqualTo(second.Plan.Path));
            Assert.That(first.Plan.Rooms.Select(r => r.TemplateId), Is.EqualTo(second.Plan.Rooms.Select(r => r.TemplateId)));
            Assert.That(first.Plan.PlayerStart, Is.EqualTo(second.Plan.PlayerStart));
            Assert.That(first.Plan.Goal, Is.EqualTo(second.Plan.Goal));
        }

        [Test]
        public void MissingTemplatesReturnFailureWithoutPartialPlan()
        {
            var closed = new RoomTemplateSnapshot("closed", RoomType.Side, RoomOpening.None, null, false);
            var result = MapPlanner.Create(4, 4, 8, 3, new[] { closed });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Attempts, Is.EqualTo(3));
            Assert.That(result.Error, Does.Contain("seed=8").And.Contain("후보 없음"));
        }

        [Test]
        public void MissingGoalMarkerAndUnverifiedPassagesAreNotAccepted()
        {
            var templates = CompleteCatalog().Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, false, t.HasPlayerStart, t.PlayerStart, false, default)).ToList();
            var result = MapPlanner.Create(4, 4, 4, 10, templates);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
        }

        [Test]
        public void UnverifiedStartOrGoalConnectionCannotBeSelected()
        {
            var unverifiedStart = CompleteCatalog().Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, t.PassagesVerified, t.HasPlayerStart, t.PlayerStart, t.HasGoal, t.Goal,
                RoomOpening.None, t.VerifiedGoalEntries)).ToList();
            var result = MapPlanner.Create(4, 4, 14, 4, unverifiedStart);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Error, Does.Contain("start=True"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrUnverifiedGoalCannotBeSelected(bool hasGoal)
        {
            var templates = CompleteCatalog().Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, t.PassagesVerified, t.HasPlayerStart, t.PlayerStart, hasGoal, t.Goal,
                t.VerifiedStartExits, RoomOpening.None)).ToList();
            var result = MapPlanner.Create(4, 4, 14, 4, templates);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("goal=True"));
        }

        [Test]
        public void UnverifiedIntermediatePassagesCannotBeSelected()
        {
            var templates = CompleteCatalog().Select(t => new RoomTemplateSnapshot(t.Id, t.Type, t.Openings,
                t.Passages, false, t.HasPlayerStart, t.PlayerStart, t.HasGoal, t.Goal,
                t.VerifiedStartExits, t.VerifiedGoalEntries)).ToList();
            var result = MapPlanner.Create(4, 4, 14, 4, templates);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
        }

        [Test]
        public void DuplicateIdsAndInvalidDimensionsFailBeforeTrying()
        {
            var t = CompleteCatalog()[0];
            var result = MapPlanner.Create(1, 4, 7, 3, new[] { t, t });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Attempts, Is.Zero);
            Assert.That(result.Error, Does.Contain("격자 크기").And.Contain("중복"));
        }

        [Test]
        public void ValidatorRejectsDanglingDoorAndForgedTemplate()
        {
            var catalog = CompleteCatalog();
            var result = MapPlanner.Create(4, 4, 42, 32, catalog);
            Assert.That(result.Success, Is.True);
            var rooms = result.Plan.Rooms.ToList();
            var old = rooms[0];
            rooms[0] = new MapRoomPlan(old.Cell, old.TemplateId, old.Type,
                old.Openings | RoomOpening.West, old.Entry, old.Exit);
            var corrupt = new MapPlan(4, 4, 42, rooms, result.Plan.Path, result.Plan.PlayerStart, result.Plan.Goal);
            Assert.That(MapPlanValidator.Validate(corrupt, catalog), Is.Not.Empty);
        }

        [Test]
        public void SnapshotCopiesPassageCollection()
        {
            var input = new List<RoomPassage> { new() { entry = RoomOpening.West, exit = RoomOpening.East } };
            var snapshot = new RoomTemplateSnapshot("room", RoomType.Horizontal,
                RoomOpening.East | RoomOpening.West, input, true);
            input.Clear();
            Assert.That(snapshot.Supports(RoomOpening.West, RoomOpening.East), Is.True);
        }

        private static List<RoomTemplateSnapshot> CompleteCatalog()
        {
            var result = new List<RoomTemplateSnapshot>();
            for (int mask = 0; mask < 16; mask++)
            {
                var openings = (RoomOpening)mask;
                var passages = new List<RoomPassage>();
                foreach (var entry in Sides)
                foreach (var exit in Sides)
                    if (entry != exit && (openings & entry) != 0 && (openings & exit) != 0)
                        passages.Add(new RoomPassage { entry = entry, exit = exit });
                var type = openings == RoomOpening.None ? RoomType.Side :
                    (openings & RoomOpening.South) != 0 ? RoomType.Down :
                    (openings & RoomOpening.North) != 0 ? RoomType.Arrival : RoomType.Horizontal;
                result.Add(new RoomTemplateSnapshot("room-" + mask, type, openings, passages, true,
                    true, new Vector3(5, 5, 0), true, new Vector3(10, 5, 0), openings, openings));
            }
            return result;
        }
    }
}
