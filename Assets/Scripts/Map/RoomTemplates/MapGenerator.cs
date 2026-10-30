namespace ProjectRE.MapGeneration
{
    using UnityEngine;

    /// <summary>한 카탈로그 스냅샷으로 계획과 씬 생성을 조율한다.</summary>
    public static class MapGenerator
    {
        public static MapBuildResult Generate(MapPlanningSettings settings, int seed,
            GameObject goalPrefab, MapBuildContext context = null)
        {
            if (settings == null || settings.Catalog == null)
                return MapBuildResult.Failed($"seed={seed}: 맵 설정 또는 카탈로그가 없습니다.");
            if (!settings.Catalog.TryCreateSnapshot(out var snapshots, out string error))
                return MapBuildResult.Failed($"seed={seed}: {error}");
            var planning = MapPlanner.Create(settings.Width, settings.Height, seed, settings.MaxAttempts, snapshots);
            if (!planning.Success) return MapBuildResult.Failed(planning.Error);
            return MapBuilder.Build(planning.Plan, settings.Catalog, goalPrefab, context, settings.VisualGround,
                settings.VisualGroundMaterial);
        }
    }
}
