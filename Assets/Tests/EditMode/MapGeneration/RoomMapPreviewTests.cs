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
                    Assert.That(monsters, Is.EqualTo(monster == null ? 0 : 1));
                    return $"{activeTriggers}:{monsters}:{count}:{setup.Find("SouthGate").gameObject.activeSelf}";
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            foreach (int seed in new[] { 0, 3, 7 }) Assert.That(Generate(seed), Is.EqualTo(Generate(seed)));
            Assert.That(Generate(0), Is.EqualTo("0:1:1:True"));
            Assert.That(Generate(3), Is.EqualTo("0:0:0:False"));
            Assert.That(Generate(7), Is.EqualTo("2:1:3:True"));
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
