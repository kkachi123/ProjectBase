namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public enum RoomTerrainPatternKind { Tilemap, Platform }

    [Serializable]
    public sealed class RoomTerrainPattern
    {
        public string id;
        public GameObject prefab;
        public RoomTerrainPatternKind kind;
        [Tooltip("변형 영역의 왼쪽 아래를 기준으로 한 플랫폼 위치입니다.")]
        public Vector2 offset;
    }

    [Serializable]
    public sealed class RoomChanceCell
    {
        public Vector2Int cell;
        public TileBase tile;
        [Range(0, 100)] public int chance = 50;
    }

    [Serializable]
    public sealed class RoomTerrainVariation
    {
        public int regionIndex;
        public bool allowEmptyPattern = true;
        public List<RoomTerrainPattern> patterns = new();
        public List<RoomChanceCell> chanceCells = new();
        [Tooltip("모든 패턴과 확률 셀 조합의 통과 방향을 실제 Player로 검증한 경우에만 활성화합니다.")]
        public bool combinationsVerified;
    }

    /// <summary>방의 변형 지형과 보호 영역을 설정한다.</summary>
    [DisallowMultipleComponent]
    public sealed class RoomTerrainSettings : MonoBehaviour
    {
        [SerializeField] private List<RectInt> _variationRegions = new();
        [SerializeField] private List<RectInt> _protectedRegions = new();
        [SerializeField] private List<RoomTerrainVariation> _terrainVariations = new();

        public IReadOnlyList<RectInt> VariationRegions => _variationRegions;
        public IReadOnlyList<RectInt> ProtectedRegions => _protectedRegions;
        public IReadOnlyList<RoomTerrainVariation> TerrainVariations => _terrainVariations;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            foreach (RectInt region in _variationRegions)
                Gizmos.DrawWireCube(new Vector3(region.center.x, region.center.y), new Vector3(region.width, region.height));
            Gizmos.color = Color.red;
            foreach (RectInt region in _protectedRegions)
                Gizmos.DrawWireCube(new Vector3(region.center.x, region.center.y), new Vector3(region.width, region.height));
        }
    }
}
