namespace ProjectRE.MapGeneration
{
    using UnityEngine;

    /// <summary>맵 생성에 필요한 제작 카탈로그와 격자 설정.</summary>
    [CreateAssetMenu(fileName = "MapPlanningSettings", menuName = "Map/Planning Settings")]
    public sealed class MapPlanningSettings : ScriptableObject
    {
        [SerializeField] private RoomTemplateCatalog _catalog;
        [SerializeField] private int _width = 4;
        [SerializeField] private int _height = 4;
        [SerializeField] private int _maxAttempts = 32;

        public RoomTemplateCatalog Catalog => _catalog;
        public int Width => _width;
        public int Height => _height;
        public int MaxAttempts => _maxAttempts;

        public MapPlanningResult CreatePlan(int seed)
        {
            if (_catalog == null) return MapPlanningResult.Failed($"seed={seed}: 템플릿 카탈로그가 없습니다.", 0);
            if (!_catalog.TryCreateSnapshot(out var templates, out string error))
                return MapPlanningResult.Failed($"seed={seed}: {error}", 0);
            return MapPlanner.Create(_width, _height, seed, _maxAttempts, templates);
        }
    }
}
