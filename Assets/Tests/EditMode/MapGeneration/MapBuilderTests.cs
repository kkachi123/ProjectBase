namespace ProjectRE.MapGeneration.Tests
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class MapBuilderTests
    {
        private const string SettingsPath = "Assets/Prefabs/@Data/Map/MapPlanningSettings.asset";
        private const string GoalPath = "Assets/Prefabs/Map/GridChunk/@BackGroundPrefab/GoalTrigger/EndLight-EndTrigger.prefab";

        [Test]
        public void BuildsExactlyThePlannedRoomsUnderAnInactiveRoot()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var plan = settings.CreatePlan(42);
            Assert.That(plan.Success, Is.True, plan.Error);
            var result = MapBuilder.Build(plan.Plan, settings.Catalog, goal);
            Assert.That(result.Success, Is.True, result.Error);
            try
            {
                Assert.That(result.Root.activeSelf, Is.False);
                Assert.That(result.Root.transform.childCount, Is.EqualTo(plan.Plan.Rooms.Count + 1));
                var instances = result.Root.GetComponentsInChildren<RoomTemplate>(true);
                Assert.That(instances.Length, Is.EqualTo(plan.Plan.Rooms.Count));
                for (int i = 0; i < instances.Length; i++)
                {
                    var room = plan.Plan.Rooms[i];
                    Assert.That(instances[i].TemplateId, Is.EqualTo(room.TemplateId));
                    Assert.That(instances[i].transform.position, Is.EqualTo(new Vector3(room.Cell.x * 20, -room.Cell.y * 20, 0)));
                }
                Assert.That(result.Root.transform.GetChild(result.Root.transform.childCount - 1).position,
                    Is.EqualTo(plan.Plan.Goal));
            }
            finally { Object.DestroyImmediate(result.Root); }
        }

        [Test]
        public void FailedInstantiationRemovesOnlyTheNewInactiveRoot()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var plan = settings.CreatePlan(17);
            var existing = new GameObject("Existing map");
            GameObject created = null;
            int calls = 0;
            var context = new MapBuildContext
            {
                CreateRoot = name => created = new GameObject(name),
                InstantiatePrefab = (prefab, parent) => ++calls == 3 ? null : Object.Instantiate(prefab, parent),
                DestroyObject = target => Object.DestroyImmediate(target),
            };
            try
            {
                var result = MapBuilder.Build(plan.Plan, settings.Catalog, goal, context);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Does.Contain("방 생성 실패"));
                Assert.That(created == null, Is.True);
                Assert.That(existing != null && existing.activeSelf, Is.True);
            }
            finally { if (existing != null) Object.DestroyImmediate(existing); }
        }

        [Test]
        public void InvalidCatalogFailsBeforeCreatingAnyRoot()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(SettingsPath);
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPath);
            var plan = settings.CreatePlan(5);
            var emptyCatalog = ScriptableObject.CreateInstance<RoomTemplateCatalog>();
            bool created = false;
            var context = new MapBuildContext { CreateRoot = name => { created = true; return new GameObject(name); } };
            try
            {
                var result = MapBuilder.Build(plan.Plan, emptyCatalog, goal, context);
                Assert.That(result.Success, Is.False);
                Assert.That(created, Is.False);
            }
            finally { Object.DestroyImmediate(emptyCatalog); }
        }
    }
}
