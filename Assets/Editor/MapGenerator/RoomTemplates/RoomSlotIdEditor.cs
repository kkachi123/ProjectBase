namespace ProjectRE.MapGeneration.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>Inspector 편집과 명시적인 복구에서만 슬롯 ID를 부여한다.</summary>
    public static class RoomSlotIdEditor
    {
        public static bool Repair(Component component)
        {
            if (!(component is RoomSpawnSettings) && !(component is RoomHazardSettings)) return false;
            var serialized = new SerializedObject(component);
            var slots = serialized.FindProperty(component is RoomHazardSettings ? "_hazards" : "_slots");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool changed = false;
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                changed |= Assign(slot.FindPropertyRelative(component is RoomHazardSettings ? "id" : "_id"), ids);
                if (component is RoomHazardSettings) continue;
                var options = slot.FindPropertyRelative("options");
                var optionIds = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < options.arraySize; j++)
                    changed |= Assign(options.GetArrayElementAtIndex(j).FindPropertyRelative("_id"), optionIds);
            }
            if (changed) serialized.ApplyModifiedProperties();
            return changed;
        }

        private static bool Assign(SerializedProperty id, HashSet<string> used)
        {
            if (!string.IsNullOrWhiteSpace(id.stringValue) && used.Add(id.stringValue)) return false;
            string value;
            do { value = Guid.NewGuid().ToString("N"); } while (!used.Add(value));
            id.stringValue = value;
            return true;
        }

        [MenuItem("Map/Room Templates/Repair Selected Slot IDs")]
        private static void RepairSelected()
        {
            var prefab = Selection.activeGameObject;
            if (prefab == null || !EditorUtility.IsPersistent(prefab) || prefab.GetComponent<RoomTemplate>() == null)
            {
                Debug.LogError("[RoomTemplate] 프로젝트 창에서 RoomTemplate 프리팹을 선택하세요.");
                return;
            }

            int changed = 0;
            foreach (var settings in prefab.GetComponents<RoomSpawnSettings>())
                if (Repair(settings)) changed++;
            var hazards = prefab.GetComponent<RoomHazardSettings>();
            if (hazards != null && Repair(hazards)) changed++;
            if (changed > 0) AssetDatabase.SaveAssetIfDirty(prefab);
            Debug.Log($"[RoomTemplate] {prefab.name}: 슬롯 ID 복구 완료 (변경된 설정 {changed}개)", prefab);
        }
    }

    [CustomEditor(typeof(RoomSpawnSettings), true)]
    public sealed class RoomSpawnSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() => RoomSlotInspector.Create(serializedObject, target as Component);
    }

    [CustomEditor(typeof(RoomHazardSettings))]
    public sealed class RoomHazardSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() => RoomSlotInspector.Create(serializedObject, target as Component);
    }

    internal static class RoomSlotInspector
    {
        public static VisualElement Create(SerializedObject serialized, Component component)
        {
            var root = new VisualElement();
            root.TrackSerializedObjectValue(serialized, _ => RoomSlotIdEditor.Repair(component));
            var iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script") continue;
                var field = new PropertyField(iterator.Copy());
                root.Add(field);
            }
            return root;
        }
    }
}
