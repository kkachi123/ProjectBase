namespace ProjectRE.MapGeneration.Tests
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public sealed class RoomMapPreviewTests
    {
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
                seed.SetValue(window, 0);
                generate.Invoke(window, null);
                Assert.That(scene.rootCount, Is.EqualTo(initialRoots + 1));
                AssertItem((GameObject)owner.GetValue(window));

                Undo.PerformUndo();
                AssertOwnedRoot(scene, owner, window);
                AssertTerrain((GameObject)owner.GetValue(window));
                Undo.PerformRedo();
                AssertOwnedRoot(scene, owner, window);
                AssertItem((GameObject)owner.GetValue(window));
                clear.Invoke(window, null);
                Assert.That(scene.rootCount, Is.EqualTo(initialRoots));
                Undo.PerformUndo();
                AssertOwnedRoot(scene, owner, window);
                AssertItem((GameObject)owner.GetValue(window));
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

        private static void AssertItem(GameObject root)
        {
            var room = Array.Find(root.GetComponentsInChildren<RoomTemplate>(true),
                candidate => candidate.TemplateId == "production.optional.ruined_court_west" && candidate.transform.localPosition == new Vector3(60, -40, 0));
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
