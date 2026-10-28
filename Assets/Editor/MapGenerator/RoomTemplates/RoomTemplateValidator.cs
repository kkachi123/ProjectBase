namespace ProjectRE.MapGeneration.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public static class RoomTemplateValidator
    {
        [MenuItem("Map/Room Templates/Validate Selected")]
        private static void ValidateSelected()
        {
            var prefab = Selection.activeGameObject;
            var template = prefab != null ? prefab.GetComponent<RoomTemplate>() : null;
            if (template == null)
            {
                Debug.LogError("[RoomTemplate] RoomTemplate 프리팹을 선택하세요.");
                return;
            }

            List<string> errors = Validate(template);
            if (errors.Count == 0)
                Debug.Log($"[RoomTemplate] {template.name}: 규격 검사 통과", template);
            else
                foreach (string error in errors)
                    Debug.LogError($"[RoomTemplate] {template.name}: {error}", template);
        }

        public static List<string> Validate(RoomTemplate template) => RoomTemplateChecks.Validate(template);
    }
}