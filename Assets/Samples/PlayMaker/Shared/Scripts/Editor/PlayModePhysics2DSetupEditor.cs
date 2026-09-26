using HutongGames.PlayMaker.Samples;
using UnityEditor;
using UnityEngine;

namespace HutongGames.PlayMaker.Editor
{
    internal static class PlayModePhysics2DSetupRestore
    {
        [InitializeOnLoadMethod]
        private static void EditorInit()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                PlayModePhysics2DSetup.RestorePlayModeOverrides();
            }
        }
    }

    [CustomPropertyDrawer(typeof(PlayModePhysics2DSetup.CollisionRule2D))]
    internal sealed class PlayModePhysics2DSetupRuleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var layerAProperty = property.FindPropertyRelative("_layerA");
            var layerBProperty = property.FindPropertyRelative("_layerB");
            var ignoreCollisionProperty = property.FindPropertyRelative("_ignoreCollision");

            position.height = EditorGUIUtility.singleLineHeight;
            position = EditorGUI.PrefixLabel(position, label);

            var previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float spacing = 4f;
            const float toggleWidth = 60f;
            var layerWidth = (position.width - toggleWidth - spacing * 2f) * 0.5f;

            var layerARect = new Rect(position.x, position.y, layerWidth, position.height);
            var layerBRect = new Rect(layerARect.xMax + spacing, position.y, layerWidth, position.height);
            var toggleRect = new Rect(layerBRect.xMax + spacing, position.y, toggleWidth, position.height);

            layerAProperty.intValue = EditorGUI.LayerField(layerARect, layerAProperty.intValue);
            layerBProperty.intValue = EditorGUI.LayerField(layerBRect, layerBProperty.intValue);
            ignoreCollisionProperty.boolValue = EditorGUI.ToggleLeft(toggleRect, "Ignore", ignoreCollisionProperty.boolValue);

            EditorGUI.indentLevel = previousIndent;
        }
    }

    [CustomEditor(typeof(PlayModePhysics2DSetup))]
    internal sealed class PlayModePhysics2DSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Applies temporary Physics2D layer rules and fixedDeltaTime while the editor is in play mode, then restores the original values when play mode ends.",
                MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
