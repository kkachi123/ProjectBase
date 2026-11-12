namespace ProjectRE.MapGeneration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public sealed class RoomMapPreviewTests
    {
        [Test]
        public void ObjectiveRulesUseActualCandidatesAndKeepTheirStatePerRoom()
        {
            var root = new GameObject("rules");
            root.SetActive(false);
            var assets = new List<ScriptableObject>();
            try
            {
                Type healthType = Type.GetType("ProjectRE.Health, Assembly-CSharp");
                Type triggerType = Type.GetType("ProjectRE.RoomTriggerObjective, Assembly-CSharp");
                Type objectiveType = Type.GetType("ProjectRE.RoomObjective, Assembly-CSharp");
                Type candidatesType = Type.GetType("ProjectRE.RoomObjectiveCandidates, Assembly-CSharp");
                Type monsterType = Type.GetType("ProjectRE.RoomMonsterObjective, Assembly-CSharp");
                var monsters = Array.CreateInstance(healthType, 2);
                for (int i = 0; i < monsters.Length; i++)
                {
                    var monster = new GameObject("monster " + i);
                    monster.transform.SetParent(root.transform);
                    monsters.SetValue(monster.AddComponent(healthType), i);
                }
                var triggers = Array.CreateInstance(triggerType, 2);
                for (int i = 0; i < triggers.Length; i++)
                {
                    var target = new GameObject("trigger " + i);
                    target.transform.SetParent(root.transform);
                    target.AddComponent<BoxCollider2D>().isTrigger = true;
                    var trigger = target.AddComponent(triggerType);
                    var serialized = new SerializedObject(trigger);
                    serialized.FindProperty("mode").enumValueIndex = i;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    target.SetActive(false);
                    triggers.SetValue(trigger, i);
                }
                object Candidates(Array m, Array t, GameObject owner, int index) =>
                    Activator.CreateInstance(candidatesType, m, t, owner, new MapBuildContext(), index);
                var first = Candidates(monsters, triggers, root, 1);
                var empty = Candidates(Array.CreateInstance(healthType, 0), Array.CreateInstance(triggerType, 0), root, 0);
                var resultType = typeof(List<>).MakeGenericType(objectiveType);
                System.Collections.IList Apply(string name, object candidates)
                {
                    var rule = ScriptableObject.CreateInstance(Type.GetType("ProjectRE." + name + ", Assembly-CSharp"));
                    assets.Add(rule);
                    Assert.That(rule.GetType().GetMethod("CanApply").Invoke(rule, new[] { empty }), Is.EqualTo(false));
                    Assert.That(rule.GetType().GetMethod("CanApply").Invoke(rule, new[] { candidates }), Is.EqualTo(true));
                    var result = (System.Collections.IList)Activator.CreateInstance(resultType);
                    rule.GetType().GetMethod("CreateObjectives").Invoke(rule, new[] { candidates, result });
                    return result;
                }
                var all = Apply("AllMonstersObjectiveRule", first);
                Assert.That(all.Count, Is.EqualTo(2));
                var one = Apply("RandomMonsterObjectiveRule", first);
                Assert.That(one.Count, Is.EqualTo(1));
                Assert.That(monsterType.GetField("target", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(one[0]), Is.SameAs(monsters.GetValue(1)));
                Assert.That(((Component)monsters.GetValue(1)).transform.Find("Objective Target"), Is.Not.Null);
                Assert.That(Apply("ReachObjectiveRule", first).Count, Is.EqualTo(1));
                Assert.That(Apply("SwitchObjectiveRule", first).Count, Is.EqualTo(1));
                Assert.That(((Component)triggers.GetValue(0)).gameObject.activeSelf, Is.True);
                Assert.That(((Component)triggers.GetValue(1)).gameObject.activeSelf, Is.True);
                Assert.That(all[0], Is.Not.SameAs(one[0]));

                foreach (object health in monsters) healthType.GetMethod("Initialize").Invoke(health, new object[] { 10f });
                var otherRoom = new GameObject("other room");
                otherRoom.transform.SetParent(root.transform);
                var otherMonster = new GameObject("other monster");
                otherMonster.transform.SetParent(otherRoom.transform);
                var otherHealth = otherMonster.AddComponent(healthType);
                healthType.GetMethod("Initialize").Invoke(otherHealth, new object[] { 10f });
                var otherMonsters = Array.CreateInstance(healthType, 1);
                otherMonsters.SetValue(otherHealth, 0);
                var otherCandidates = Candidates(otherMonsters, Array.CreateInstance(triggerType, 0), otherRoom, 0);
                var otherGoals = (System.Collections.IList)Activator.CreateInstance(resultType);
                assets[0].GetType().GetMethod("CreateObjectives").Invoke(assets[0], new[] { otherCandidates, otherGoals });
                var start = monsterType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (object objective in all) start.Invoke(objective, null);
                start.Invoke(one[0], null);
                start.Invoke(otherGoals[0], null);
                healthType.GetMethod("TakeDamage").Invoke(monsters.GetValue(1), new object[] { 10f });
                var complete = objectiveType.GetProperty("IsComplete");
                Assert.That(complete.GetValue(all[0]), Is.EqualTo(false));
                Assert.That(complete.GetValue(all[1]), Is.EqualTo(true));
                Assert.That(complete.GetValue(one[0]), Is.EqualTo(true));
                Assert.That(complete.GetValue(otherGoals[0]), Is.EqualTo(false));
                Assert.That(((Component)monsters.GetValue(1)).transform.Find("Objective Target").gameObject.activeSelf, Is.False);
                healthType.GetMethod("TakeDamage").Invoke(monsters.GetValue(0), new object[] { 10f });
                Assert.That(complete.GetValue(all[0]), Is.EqualTo(true));
                var disable = monsterType.GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (object objective in all) disable.Invoke(objective, null);
                disable.Invoke(one[0], null);
                disable.Invoke(otherGoals[0], null);
            }
            finally
            {
                foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(RoomOpening.North)]
        [TestCase(RoomOpening.East)]
        [TestCase(RoomOpening.South)]
        [TestCase(RoomOpening.West)]
        public void ObjectiveSetupLocksOnlyThePlannedExit(RoomOpening exit)
        {
            var root = new GameObject("room");
            root.SetActive(false);
            try
            {
                root.AddComponent<Grid>();
                var room = root.AddComponent<RoomTemplate>();
                var roomData = new SerializedObject(room);
                roomData.FindProperty("_templateId").stringValue = "test";
                roomData.ApplyModifiedPropertiesWithoutUndo();
                var group = new GameObject("objectives");
                group.transform.SetParent(root.transform, false);
                var controller = group.AddComponent(Type.GetType("ProjectRE.RoomObjectiveController, Assembly-CSharp"));
                var setup = group.AddComponent(Type.GetType("ProjectRE.RoomObjectiveSetup, Assembly-CSharp"));
                var target = new GameObject("trigger");
                target.transform.SetParent(group.transform, false);
                target.AddComponent<BoxCollider2D>().isTrigger = true;
                var trigger = target.AddComponent(Type.GetType("ProjectRE.RoomTriggerObjective, Assembly-CSharp"));
                target.SetActive(false);
                var serialized = new SerializedObject(setup);
                serialized.FindProperty("controller").objectReferenceValue = controller;
                serialized.FindProperty("conditionChance").intValue = 100;
                var rule = ScriptableObject.CreateInstance(Type.GetType("ProjectRE.ReachObjectiveRule, Assembly-CSharp"));
                var rules = serialized.FindProperty("rules");
                rules.arraySize = 1;
                rules.GetArrayElementAtIndex(0).objectReferenceValue = rule;
                var triggers = serialized.FindProperty("triggers");
                triggers.arraySize = 1;
                triggers.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "always";
                triggers.GetArrayElementAtIndex(0).FindPropertyRelative("objective").objectReferenceValue = trigger;
                triggers.GetArrayElementAtIndex(0).FindPropertyRelative("chance").intValue = 100;
                var gates = serialized.FindProperty("gates");
                var directions = new[] { RoomOpening.North, RoomOpening.East, RoomOpening.South, RoomOpening.West };
                var gateObjects = new List<GameObject>();
                gates.arraySize = directions.Length;
                for (int i = 0; i < directions.Length; i++)
                {
                    var gate = new GameObject(directions[i].ToString());
                    gate.transform.SetParent(group.transform, false);
                    gate.AddComponent<BoxCollider2D>();
                    gate.SetActive(false);
                    gateObjects.Add(gate);
                    gates.GetArrayElementAtIndex(i).FindPropertyRelative("direction").intValue = (int)directions[i];
                    gates.GetArrayElementAtIndex(i).FindPropertyRelative("gate").objectReferenceValue = gate;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var entry = exit == RoomOpening.South ? RoomOpening.North :
                    exit == RoomOpening.West ? RoomOpening.East : RoomOpening.West;
                var branches = exit == RoomOpening.South ? RoomOpening.East | RoomOpening.West : RoomOpening.None;
                var plan = new MapRoomPlan(Vector2Int.zero, "test", RoomType.Horizontal, entry | exit | branches,
                    entry, exit, branch: branches);
                setup.GetType().GetMethod("Configure").Invoke(setup,
                    new object[] { new BuiltRoom(plan, room, new Dictionary<int, GameObject>()), 42, new MapBuildContext() });
                for (int i = 0; i < directions.Length; i++)
                    Assert.That(gateObjects[i].activeSelf, Is.EqualTo(directions[i] == exit));
                Assert.That(target.activeSelf, Is.True);
                UnityEngine.Object.DestroyImmediate(rule);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ObjectiveCandidatesFollowSpawnedContentAndSeed()
        {
            var factory = Type.GetType("ProjectRE.RoomMapFactory, Assembly-CSharp");
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Goals/GoalTrigger/EndLight-EndTrigger.prefab");
            Assert.That(factory, Is.Not.Null);
            string Generate(int seed)
            {
                object[] args = { settings, seed, goal, new MapBuildContext(), null, null, null };
                Assert.That(factory.GetMethod("TryCreate").Invoke(null, args), Is.EqualTo(true), args[6]?.ToString());
                var root = ((Component)args[4]).gameObject;
                try
                {
                    var room = Array.Find(root.GetComponentsInChildren<RoomTemplate>(true),
                        item => item.TemplateId == "production.down.west_forest_step");
                    Assert.That(room, Is.Not.Null);
                    var setup = room.transform.Find("RoomObjectives");
                    Assert.That(setup, Is.Not.Null);
                    var controller = setup.GetComponent("RoomObjectiveController");
                    int count = (int)controller.GetType().GetProperty("TotalCount").GetValue(controller);
                    var triggerType = Type.GetType("ProjectRE.RoomTriggerObjective, Assembly-CSharp");
                    var triggers = setup.GetComponentsInChildren(triggerType, true);
                    int activeTriggers = 0;
                    foreach (Component trigger in triggers) if (trigger.gameObject.activeSelf) activeTriggers++;
                    int monsters = setup.GetComponents(Type.GetType("ProjectRE.RoomMonsterObjective, Assembly-CSharp")).Length;
                    Assert.That(count, Is.EqualTo(activeTriggers + monsters));
                    Assert.That(setup.Find("SouthGate").gameObject.activeSelf, Is.EqualTo(count > 0));
                    var monster = room.GetComponentInChildren(Type.GetType("ProjectRE.MonsterController, Assembly-CSharp"), true);
                    Assert.That(monsters == 0 || monster != null, Is.True);
                    Assert.That(activeTriggers == 0 || monsters == 0, Is.True);
                    return $"{activeTriggers}:{monsters}:{count}:{setup.Find("SouthGate").gameObject.activeSelf}";
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var results = new HashSet<string>();
            int checkedSeeds = 0;
            for (int seed = 0; seed < 500 && checkedSeeds < 12; seed++)
            {
                var plan = settings.CreatePlan(seed);
                if (!System.Linq.Enumerable.Any(plan.Plan.Rooms, room => room.TemplateId == "production.down.west_forest_step")) continue;
                string result = Generate(seed);
                Assert.That(Generate(seed), Is.EqualTo(result));
                results.Add(result);
                checkedSeeds++;
            }
            Assert.That(checkedSeeds, Is.EqualTo(12));
            Assert.That(results.Contains("0:0:0:False"), Is.True);
            Assert.That(results.Count, Is.GreaterThan(1));
        }

        [Test]
        public void BranchRoomOnlyLocksItsPlannedExit()
        {
            var factory = Type.GetType("ProjectRE.RoomMapFactory, Assembly-CSharp");
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Goals/GoalTrigger/EndLight-EndTrigger.prefab");
            bool verified = false;
            for (int seed = 0; seed < 250 && !verified; seed++)
            {
                var plan = settings.CreatePlan(seed).Plan;
                var branches = System.Linq.Enumerable.Where(plan.Rooms,
                    room => room.TemplateId == "nova.variant.junction_down" && room.Branch != RoomOpening.None);
                if (!System.Linq.Enumerable.Any(branches)) continue;
                object[] args = { settings, seed, goal, new MapBuildContext(), null, null, null };
                Assert.That(factory.GetMethod("TryCreate").Invoke(null, args), Is.EqualTo(true), args[6]?.ToString());
                var root = ((Component)args[4]).gameObject;
                try
                {
                    foreach (var planned in branches)
                    {
                        var room = Array.Find(root.GetComponentsInChildren<RoomTemplate>(true),
                            item => item.transform.localPosition == new Vector3(planned.Cell.x * 20, -planned.Cell.y * 20, 0));
                        var group = room.transform.Find("RoomObjectives");
                        var controller = group.GetComponent("RoomObjectiveController");
                        int count = (int)controller.GetType().GetProperty("TotalCount").GetValue(controller);
                        if (count == 0) continue;
                        foreach (var direction in new[] { RoomOpening.West, RoomOpening.East, RoomOpening.South })
                        {
                            bool locked = group.Find(direction + "Gate").gameObject.activeSelf;
                            Assert.That(locked, Is.EqualTo(direction == planned.Exit));
                            if ((planned.Branch & direction) != 0) Assert.That(locked, Is.False);
                        }
                        verified = true;
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            Assert.That(verified, Is.True, "목표가 있는 분기 방이 생성되지 않았습니다.");
        }

        [Test]
        public void PreviewWithTerrainVisualRestoresItsTilesAndSourceRenderersAfterUndoRedo()
        {
            Scene scene = SceneManager.GetActiveScene();
            int initialRoots = scene.rootCount;
            EditorWindow window = null;
            try
            {
                Type type = Type.GetType("ProjectRE.MapGeneration.Editor.RoomMapGeneratorWindow, Assembly-CSharp-Editor");
                Assert.That(type, Is.Not.Null);
                window = (EditorWindow)ScriptableObject.CreateInstance(type);
                type.GetMethod("CreateGUI").Invoke(window, null);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>(
                    "Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
                Assert.That(settings, Is.Not.Null);
                type.GetField("_settings", flags).SetValue(window, settings);
                type.GetField("_seed", flags).SetValue(window, 42);
                var owner = type.GetField("_previewRoot", flags);
                type.GetMethod("Generate", flags).Invoke(window, null);
                var root = (GameObject)owner.GetValue(window);
                AssertVisual(root, settings.VisualGround, settings.VisualGroundMaterial);

                Undo.PerformUndo();
                Assert.That(scene.rootCount, Is.EqualTo(initialRoots));
                Undo.PerformRedo();
                AssertOwnedRoot(scene, owner, window);
                AssertVisual((GameObject)owner.GetValue(window), settings.VisualGround, settings.VisualGroundMaterial);
            }
            finally
            {
                if (window != null)
                {
                    var owned = (System.Collections.IEnumerable)window.GetType()
                        .GetField("_ownedRoots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                    foreach (GameObject root in owned)
                        if (root != null) UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }

        [Test]
        public void PreviewUndoRedoRestoresRandomMonsterTargetMarker()
        {
            EditorWindow window = null;
            try
            {
                Type type = Type.GetType("ProjectRE.MapGeneration.Editor.RoomMapGeneratorWindow, Assembly-CSharp-Editor");
                window = (EditorWindow)ScriptableObject.CreateInstance(type);
                type.GetMethod("CreateGUI").Invoke(window, null);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                type.GetField("_settings", flags).SetValue(window,
                    AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset"));
                type.GetField("_seed", flags).SetValue(window, 18);
                var owner = type.GetField("_previewRoot", flags);
                type.GetMethod("Generate", flags).Invoke(window, null);
                bool HasMarker() => Array.Exists(((GameObject)owner.GetValue(window)).GetComponentsInChildren<Transform>(true),
                    item => item.name == "Objective Target");
                Assert.That(HasMarker(), Is.True);
                Undo.PerformUndo();
                Undo.PerformRedo();
                Assert.That(HasMarker(), Is.True);
            }
            finally
            {
                if (window != null)
                {
                    var owned = (System.Collections.IEnumerable)window.GetType()
                        .GetField("_ownedRoots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                    foreach (GameObject root in owned)
                        if (root != null) UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }

        private static void AssertVisual(GameObject root, UnityEngine.Tilemaps.TileBase ground, Material material)
        {
            Assert.That(root, Is.Not.Null);
            var child = root.transform.Find("Terrain Visual Grid/Terrain Visual");
            Assert.That(child, Is.Not.Null);
            var map = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            Assert.That(map, Is.Not.Null);
            Assert.That(child.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sharedMaterial, Is.SameAs(material));
            bool foundTile = false;
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(cell)) continue;
                Assert.That(map.GetTile(cell), Is.SameAs(ground));
                foundTile = true;
                break;
            }
            Assert.That(foundTile, Is.True);
            foreach (var room in root.GetComponentsInChildren<RoomTemplate>(true))
                Assert.That(room.FixedGeometry.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().enabled, Is.False);
        }

        [Test]
        public void ReplaceAndClearPreviewCanBeUndoneWithoutLosingOwnership()
        {
            Scene scene = SceneManager.GetActiveScene();
            int initialRoots = scene.rootCount;
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            int itemSeed = -1;
            Vector2Int itemCell = default;
            for (int candidateSeed = 0; candidateSeed < 1000 && itemSeed < 0; candidateSeed++)
            {
                var candidate = settings.CreatePlan(candidateSeed);
                Assert.That(candidate.Success, Is.True, candidate.Error);
                foreach (var room in candidate.Plan.Rooms)
                foreach (var content in room.Content)
                    if (room.IsOptional && content.OptionId == "item") { itemSeed = candidateSeed; itemCell = room.Cell; break; }
            }
            Assert.That(itemSeed, Is.GreaterThanOrEqualTo(0));
            EditorWindow window = null;
            try
            {
                Type type = Type.GetType("ProjectRE.MapGeneration.Editor.RoomMapGeneratorWindow, Assembly-CSharp-Editor");
                Assert.That(type, Is.Not.Null);
                window = (EditorWindow)ScriptableObject.CreateInstance(type);
                type.GetMethod("CreateGUI").Invoke(window, null);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo generate = type.GetMethod("Generate", flags);
                MethodInfo clear = type.GetMethod("ClearPreview", flags);
                FieldInfo owner = type.GetField("_previewRoot", flags);
                FieldInfo seed = type.GetField("_seed", flags);

                seed.SetValue(window, 3);
                generate.Invoke(window, null);
                var first = (GameObject)owner.GetValue(window);
                Assert.That(first != null && first.activeSelf, Is.True);
                Assert.That(first.GetComponent("GeneratedMapInfo"), Is.Not.Null);
                AssertTerrain(first);
                seed.SetValue(window, itemSeed);
                generate.Invoke(window, null);
                Assert.That(scene.rootCount, Is.EqualTo(initialRoots + 1));
                AssertItem((GameObject)owner.GetValue(window), itemCell);

                Undo.PerformUndo();
                AssertOwnedRoot(scene, owner, window);
                AssertTerrain((GameObject)owner.GetValue(window));
                Undo.PerformRedo();
                AssertOwnedRoot(scene, owner, window);
                AssertItem((GameObject)owner.GetValue(window), itemCell);
                clear.Invoke(window, null);
                Assert.That(scene.rootCount, Is.EqualTo(initialRoots));
                Undo.PerformUndo();
                AssertOwnedRoot(scene, owner, window);
                AssertItem((GameObject)owner.GetValue(window), itemCell);
            }
            finally
            {
                if (window != null)
                {
                    var owned = (System.Collections.IEnumerable)window.GetType()
                        .GetField("_ownedRoots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                    foreach (GameObject root in owned)
                        if (root != null) UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }

        private static void AssertItem(GameObject root, Vector2Int cell)
        {
            var room = Array.Find(root.GetComponentsInChildren<RoomTemplate>(true),
                candidate => candidate.transform.localPosition == new Vector3(cell.x * 20, -cell.y * 20, 0));
            Assert.That(room, Is.Not.Null);
            var components = room.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.That(Array.Exists(components, component => component != null && component.GetType().Name == "ItemPickup"), Is.True);
        }

        private static void AssertTerrain(GameObject root)
        {
            var room = Array.Find(root.GetComponentsInChildren<RoomTemplate>(true),
                candidate => candidate.TemplateId == "production.horizontal.step" && candidate.transform.localPosition == new Vector3(20, -20, 0));
            Assert.That(room, Is.Not.Null);
            Assert.That(room.FixedGeometry.HasTile(new Vector3Int(3, 12, 0)), Is.True);
            Assert.That(room.GetComponentsInChildren<Grid>(true).Length, Is.EqualTo(2));
        }

        private static void AssertOwnedRoot(Scene scene, FieldInfo owner, EditorWindow window)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            var root = (GameObject)owner.GetValue(window);
            Assert.That(root != null && Array.Exists(roots, candidate => candidate == root), Is.True);
            Assert.That(root.activeSelf, Is.True);
            Assert.That(root.GetComponent("GeneratedMapInfo"), Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<RoomTemplate>(true).Length, Is.EqualTo(16));
        }
    }
}
