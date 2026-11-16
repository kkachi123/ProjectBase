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

        public static List<string> Validate(RoomTemplate template)
        {
            var errors = RoomTemplateChecks.Validate(template);
            errors.AddRange(ProjectRE.RoomObjectiveSetup.ValidateTemplate(template));
            return errors;
        }

        [MenuItem("Map/Room Templates/Validate Catalog")]
        private static void ValidateCatalog()
        {
            var settings = Selection.activeObject as MapPlanningSettings ??
                AssetDatabase.LoadAssetAtPath<MapPlanningSettings>("Assets/Prefabs/@Data/Map/MapPlanningSettings.asset");
            if (settings == null || settings.Catalog == null)
            {
                Debug.LogError("[RoomTemplate] 맵 설정 또는 카탈로그가 없습니다.");
                return;
            }
            int errorCount = 0;
            bool mayNeedGate = false;
            foreach (var template in settings.Catalog.Templates)
            {
                foreach (var error in Validate(template))
                {
                    errorCount++;
                    Debug.LogError($"[RoomTemplate] {(template == null ? "null" : template.name)}: {error}", template);
                }
                var setup = template == null ? null : template.GetComponentInChildren<ProjectRE.RoomObjectiveSetup>(true);
                if (setup != null && setup.HasEnabledRules) mayNeedGate = true;
            }
            if (errorCount == 0 && !settings.Catalog.TryCreateSnapshot(out _, out string catalogError))
            {
                errorCount++;
                Debug.LogError($"[RoomTemplate] {catalogError}", settings.Catalog);
            }
            if (mayNeedGate && !ProjectRE.RoomObjectiveSetup.IsValidGatePrefab(settings.ExitGatePrefab))
                Debug.LogWarning("[RoomTemplate] 목표가 생성될 수 있으므로 활성 출구 문 프리팹을 설정하세요.", settings);
            if (errorCount == 0) Debug.Log($"[RoomTemplate] {settings.Catalog.name}: 카탈로그 규격 검사 통과", settings);
        }
    }
}
