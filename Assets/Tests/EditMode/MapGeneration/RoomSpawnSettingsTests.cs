namespace ProjectRE.MapGeneration.Tests
{
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.TestTools;

    public sealed class RoomSpawnSettingsTests
    {
        [Test]
        public void CatalogKeepsCandidatesOnVariantsAndCoversMainPathRooms()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            Assert.That(settings.Catalog.TryCreateSnapshot(out var snapshots, out var error), Is.True, error);
            var variants = settings.Catalog.Templates.ToDictionary(t => t.TemplateId);
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 1000; seed++)
            {
                var result = MapPlanner.Create(settings.Width, settings.Height, seed, settings.MaxAttempts, snapshots);
                Assert.That(result.Success, Is.True, $"seed {seed}: {result.Error}");
                for (int i = 1; i < result.Plan.Path.Count - 1; i++)
                    seen.Add(result.Plan.Rooms.Single(r => r.Cell == result.Plan.Path[i]).TemplateId);
            }
            Assert.That(seen.Count, Is.EqualTo(11));
            foreach (string id in seen)
            {
                var room = variants[id];
                Assert.That(room.GetComponent<RoomMonsterSettings>().Slots, Has.Count.EqualTo(1), id);
                Assert.That(room.GetComponent<RoomItemSettings>().Slots, Has.Count.EqualTo(1), id);
            }
            foreach (var room in variants.Values)
            {
                var parent = PrefabUtility.GetCorrespondingObjectFromSource(room.gameObject);
                Assert.That(parent, Is.Not.Null, room.name);
                Assert.That(parent.GetComponent<RoomMonsterSettings>().Slots, Is.Empty, parent.name);
                Assert.That(parent.GetComponent<RoomItemSettings>().Slots, Is.Empty, parent.name);
                Assert.That(parent.GetComponent<RoomHazardSettings>().Hazards, Is.Empty, parent.name);
                Assert.That(parent.transform.Find("RoomObjectives"), Is.Null, parent.name);
            }
        }

        [Test]
        public void SeparateSlotsAreReproducibleAndBuilderReturnsOnlyMonsters()
        {
            var source = AssetDatabase.LoadAssetAtPath<RoomTemplate>("Assets/Prefabs/Map/RoomTemplates/Base/WestSouth.prefab");
            var room = Object.Instantiate(source);
            var catalog = ScriptableObject.CreateInstance<RoomTemplateCatalog>();
            try
            {
                var monster = new RoomContentOption { prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Goblin/Goblin.prefab"), offset = new Vector2(.5f, .05f) };
                var item = new RoomContentOption { prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/Inventory/Item.prefab"), offset = new Vector2(.5f, .3135f) };
                var hazards = new SerializedObject(room.Hazard);
                hazards.FindProperty("_hazards").arraySize = 0;
                hazards.ApplyModifiedPropertiesWithoutUndo();

                var monsters = room.GetComponent<RoomMonsterSettings>();
                var items = room.GetComponent<RoomItemSettings>();
                AddSlot(monsters, 7, monster);
                AddSlot(items, 10, item);
                var serializedCatalog = new SerializedObject(catalog);
                var templates = serializedCatalog.FindProperty("_templates");
                templates.arraySize = 1;
                templates.GetArrayElementAtIndex(0).objectReferenceValue = room;
                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(catalog.TryCreateSnapshot(out var snapshots, out var error), Is.True, error);
                var snapshot = snapshots.Single();
                Assert.That(snapshot.Content.Count, Is.EqualTo(2));
                Assert.That(snapshot.Content.SelectMany(s => s.Options).Select(o => o.Kind),
                    Is.EquivalentTo(new[] { ContentKind.Monster, ContentKind.Item }));

                var combination = new int[4];
                for (int seed = 0; seed < 10000; seed++)
                {
                    var selected = RoomContentPlanner.Select(seed, Vector2Int.zero, snapshot, ContentRoomRole.MainPath);
                    var repeated = RoomContentPlanner.Select(seed, Vector2Int.zero, snapshot, ContentRoomRole.MainPath);
                    Assert.That(selected.Select(s => (s.SlotIndex, s.OptionId)),
                        Is.EqualTo(repeated.Select(s => (s.SlotIndex, s.OptionId))));
                    int flags = 0;
                    foreach (var choice in selected)
                    {
                        if (choice.OptionId == "") continue;
                        var kind = snapshot.Content.Single(s => s.SlotIndex == choice.SlotIndex).Options.Single().Kind;
                        flags |= kind == ContentKind.Monster ? 1 : 2;
                    }
                    combination[flags]++;
                }
                foreach (int count in combination) Assert.That(count, Is.InRange(2200, 2800));

                int bothSeed = Enumerable.Range(0, 10000).First(seed =>
                    RoomContentPlanner.Select(seed, Vector2Int.zero, snapshot, ContentRoomRole.MainPath).All(s => s.OptionId != ""));
                var choices = RoomContentPlanner.Select(bothSeed, Vector2Int.zero, snapshot, ContentRoomRole.MainPath);
                var plan = new MapRoomPlan(Vector2Int.zero, snapshot.Id, snapshot.Type, snapshot.Openings,
                    RoomOpening.West, RoomOpening.South, content: choices);
                var spawnedMonsters = new List<GameObject>();
                var builder = typeof(RoomTemplate).Assembly.GetType("ProjectRE.MapGeneration.RoomContentBuilder");
                var built = (Dictionary<int, GameObject>)builder.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { plan, room, new MapBuildContext(), spawnedMonsters });
                Assert.That(built.Count, Is.EqualTo(2));
                Assert.That(spawnedMonsters.Count, Is.EqualTo(1));
                Assert.That(spawnedMonsters[0].name, Does.StartWith(monster.prefab.name));

                AddSlot(items, 3, item, append: true);
                Assert.That(catalog.TryCreateSnapshot(out var beforeReorder, out error), Is.True, error);
                var serializedItems = new SerializedObject(items);
                serializedItems.FindProperty("_slots").MoveArrayElement(0, 1);
                serializedItems.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(catalog.TryCreateSnapshot(out var afterReorder, out error), Is.True, error);
                for (int seed = 0; seed < 100; seed++)
                {
                    var before = RoomContentPlanner.Select(seed, Vector2Int.zero, beforeReorder.Single(), ContentRoomRole.MainPath);
                    var after = RoomContentPlanner.Select(seed, Vector2Int.zero, afterReorder.Single(), ContentRoomRole.MainPath);
                    Assert.That(after.Select(s => (s.SlotIndex, s.OptionId)),
                        Is.EqualTo(before.Select(s => (s.SlotIndex, s.OptionId))));
                }
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(room.gameObject);
            }
        }

        [Test]
        public void DuplicateSlotsReceiveDifferentStableIds()
        {
            var root = new GameObject("slots");
            try
            {
                var settings = root.AddComponent<RoomItemSettings>();
                var serialized = new SerializedObject(settings);
                var slots = serialized.FindProperty("_slots");
                slots.arraySize = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Repair(settings);
                var first = settings.Slots[0].Id;
                Assert.That(first, Is.Not.Empty);
                serialized.Update();
                slots = serialized.FindProperty("_slots");
                slots.InsertArrayElementAtIndex(0);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Repair(settings);
                Assert.That(settings.Slots.Select(s => s.Id).Distinct().Count(), Is.EqualTo(2));
                var before = settings.Slots.Select(s => s.Id).OrderBy(s => s).ToArray();
                serialized.Update();
                serialized.FindProperty("_slots").MoveArrayElement(0, 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(settings.Slots.Select(s => s.Id).OrderBy(s => s), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SlotIdsPersistWhenPrefabIsSavedAndDuplicated()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/MapGeneration/RoomSpawnTemporary.prefab");
            var root = new GameObject("slots");
            try
            {
                var settings = root.AddComponent<RoomItemSettings>();
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_slots").arraySize = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Repair(settings);
                string originalId = settings.Slots[0].Id;
                Assert.That(originalId, Is.Not.Empty);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoomItemSettings>();
                Assert.That(saved.Slots[0].Id, Is.EqualTo(originalId));

                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var copied = contents.GetComponent<RoomItemSettings>();
                    var edit = new SerializedObject(copied);
                    edit.FindProperty("_slots").InsertArrayElementAtIndex(1);
                    edit.ApplyModifiedPropertiesWithoutUndo();
                    Repair(copied);
                    Assert.That(copied.Slots.Select(s => s.Id).Distinct().Count(), Is.EqualTo(2));
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }

                saved = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoomItemSettings>();
                Assert.That(saved.Slots.Count, Is.EqualTo(2));
                Assert.That(saved.Slots[0].Id, Is.EqualTo(originalId));
                Assert.That(saved.Slots[1].Id, Is.Not.EqualTo(originalId));
            }
            finally
            {
                Object.DestroyImmediate(root);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void InvalidSavedIdsStayInvalidUntilRepairedAndSaved()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/MapGeneration/RoomIdRepairTemporary.prefab");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/RoomTemplates/Base/WestSouth.prefab");
            var root = Object.Instantiate(source);
            try
            {
                var items = root.GetComponent<RoomItemSettings>();
                var serialized = new SerializedObject(items);
                var slots = serialized.FindProperty("_slots");
                slots.arraySize = 2;
                slots.GetArrayElementAtIndex(0).FindPropertyRelative("_id").stringValue = "stable-id";
                slots.GetArrayElementAtIndex(1).FindPropertyRelative("_id").stringValue = "stable-id";
                var options = slots.GetArrayElementAtIndex(0).FindPropertyRelative("options");
                options.arraySize = 1;
                options.GetArrayElementAtIndex(0).FindPropertyRelative("_id").stringValue = "";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var hazards = new SerializedObject(root.GetComponent<RoomHazardSettings>());
                hazards.FindProperty("_hazards").arraySize = 1;
                hazards.FindProperty("_hazards").GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "";
                hazards.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                items = saved.GetComponent<RoomItemSettings>();
                var hazardSettings = saved.GetComponent<RoomHazardSettings>();
                Assert.That(items.Slots.Select(s => s.Id), Is.EqualTo(new[] { "stable-id", "stable-id" }));
                Assert.That(items.Slots[0].options[0].Id, Is.Empty);
                Assert.That(hazardSettings.Hazards[0].id, Is.Empty);
                var errors = RoomTemplateChecks.Validate(saved.GetComponent<RoomTemplate>());
                Assert.That(errors.Any(e => e.Contains("아이템 슬롯 #2") && e.Contains("ID")), Is.True);
                Assert.That(errors.Any(e => e.Contains("아이템 슬롯 #1 후보 #1") && e.Contains("ID")), Is.True);
                Assert.That(errors.Any(e => e.Contains("함정 슬롯 #1") && e.Contains("ID")), Is.True);

                Repair(items);
                Repair(hazardSettings);
                string second = items.Slots[1].Id;
                string option = items.Slots[0].options[0].Id;
                string hazard = hazardSettings.Hazards[0].id;
                Assert.That(items.Slots[0].Id, Is.EqualTo("stable-id"));
                Assert.That(second, Is.Not.Empty.And.Not.EqualTo("stable-id"));
                Assert.That(option, Is.Not.Empty);
                Assert.That(hazard, Is.Not.Empty);
                AssetDatabase.SaveAssetIfDirty(saved);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                items = saved.GetComponent<RoomItemSettings>();
                Assert.That(items.Slots.Select(s => s.Id), Is.EqualTo(new[] { "stable-id", second }));
                Assert.That(items.Slots[0].options[0].Id, Is.EqualTo(option));
                Assert.That(saved.GetComponent<RoomHazardSettings>().Hazards[0].id, Is.EqualTo(hazard));
            }
            finally
            {
                Object.DestroyImmediate(root);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void RepairDoesNotChangeValidCatalogIds()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            var type = System.Type.GetType("ProjectRE.MapGeneration.Editor.RoomSlotIdEditor, Assembly-CSharp-Editor");
            Assert.That(type, Is.Not.Null);
            var repair = type.GetMethod("Repair");
            foreach (var room in settings.Catalog.Templates)
            {
                foreach (var component in room.GetComponents<RoomSpawnSettings>())
                    Assert.That((bool)repair.Invoke(null, new object[] { component }), Is.False, room.name);
                Assert.That((bool)repair.Invoke(null, new object[] { room.Hazard }), Is.False, room.name);
            }
        }

        [Test]
        public void RepairMenuSavesVariantWithoutChangingOtherIdsOrParent()
        {
            const string source = "Assets/Prefabs/Map/RoomTemplates/NovaVariants/Down_ForestStep.prefab";
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/MapGeneration/RoomIdVariantTemporary.prefab");
            var previousSelection = Selection.objects;
            try
            {
                Assert.That(AssetDatabase.CopyAsset(source, path), Is.True);
                var variant = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var parent = PrefabUtility.GetCorrespondingObjectFromSource(variant);
                var monster = variant.GetComponent<RoomMonsterSettings>().Slots[0].Id;
                var items = variant.GetComponent<RoomItemSettings>();
                var serialized = new SerializedObject(items);
                serialized.FindProperty("_slots").GetArrayElementAtIndex(0).FindPropertyRelative("_id").stringValue = "";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(variant);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                variant = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(variant.GetComponent<RoomItemSettings>().Slots[0].Id, Is.Empty);
                Selection.activeGameObject = variant;
                Assert.That(EditorApplication.ExecuteMenuItem("Map/Room Templates/Repair Selected Slot IDs"), Is.True);
                string repaired = variant.GetComponent<RoomItemSettings>().Slots[0].Id;
                Assert.That(repaired, Is.Not.Empty);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                variant = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(variant.GetComponent<RoomItemSettings>().Slots[0].Id, Is.EqualTo(repaired));
                Assert.That(variant.GetComponent<RoomMonsterSettings>().Slots[0].Id, Is.EqualTo(monster));
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(variant), Is.EqualTo(parent));
            }
            finally
            {
                Selection.objects = previousSelection;
                AssetDatabase.DeleteAsset(path);
            }
        }

        [UnityTest]
        public IEnumerator InspectorAssignsIdsWhenSlotsAreEdited()
        {
            var root = new GameObject("inspector");
            var settings = root.AddComponent<RoomItemSettings>();
            var editor = UnityEditor.Editor.CreateEditor(settings);
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                window.ShowUtility();
                var inspector = editor.CreateInspectorGUI();
                window.rootVisualElement.Add(inspector);
                inspector.Bind(editor.serializedObject);
                yield return null;
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_slots").arraySize = 1;
                serialized.ApplyModifiedProperties();
                double deadline = EditorApplication.timeSinceStartup + 2;
                while (string.IsNullOrEmpty(settings.Slots[0].Id) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(settings.Slots[0].Id, Is.Not.Empty);
                string id = settings.Slots[0].Id;
                serialized.Update();
                serialized.FindProperty("_slots").GetArrayElementAtIndex(0).FindPropertyRelative("options").arraySize = 1;
                serialized.ApplyModifiedProperties();
                deadline = EditorApplication.timeSinceStartup + 2;
                while (string.IsNullOrEmpty(settings.Slots[0].options[0].Id) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(settings.Slots[0].Id, Is.EqualTo(id));
                Assert.That(settings.Slots[0].options[0].Id, Is.Not.Empty);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator InspectorDoesNotRepairInvalidIdsUntilEdited()
        {
            var root = new GameObject("inspector");
            var settings = root.AddComponent<RoomHazardSettings>();
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_hazards").arraySize = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var editor = UnityEditor.Editor.CreateEditor(settings);
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                window.ShowUtility();
                var inspector = editor.CreateInspectorGUI();
                window.rootVisualElement.Add(inspector);
                inspector.Bind(editor.serializedObject);
                double deadline = EditorApplication.timeSinceStartup + .25;
                while (EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(settings.Hazards[0].id, Is.Empty);
                serialized.Update();
                serialized.FindProperty("_hazards").GetArrayElementAtIndex(0).FindPropertyRelative("emptyWeight").intValue = 2;
                serialized.ApplyModifiedProperties();
                deadline = EditorApplication.timeSinceStartup + 2;
                while (string.IsNullOrEmpty(settings.Hazards[0].id) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(settings.Hazards[0].id, Is.Not.Empty);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        private static void AddSlot(RoomSpawnSettings settings, int x, RoomContentOption original, bool append = false)
        {
            var serialized = new SerializedObject(settings);
            var slots = serialized.FindProperty("_slots");
            int index = append ? slots.arraySize : 0;
            slots.arraySize = index + 1;
            var slot = slots.GetArrayElementAtIndex(index);
            slot.FindPropertyRelative("position").vector2IntValue = new Vector2Int(x, 4);
            slot.FindPropertyRelative("clearance").rectIntValue = new RectInt(x, 4, 2, 3);
            slot.FindPropertyRelative("emptyWeight").intValue = 1;
            var options = slot.FindPropertyRelative("options");
            options.arraySize = 1;
            var option = options.GetArrayElementAtIndex(0);
            option.FindPropertyRelative("prefab").objectReferenceValue = original.prefab;
            option.FindPropertyRelative("weight").intValue = 1;
            option.FindPropertyRelative("offset").vector2Value = original.offset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Repair(settings);
        }

        private static void Repair(Component component)
        {
            var type = System.Type.GetType("ProjectRE.MapGeneration.Editor.RoomSlotIdEditor, Assembly-CSharp-Editor");
            Assert.That(type, Is.Not.Null);
            type.GetMethod("Repair").Invoke(null, new object[] { component });
        }
    }
}
