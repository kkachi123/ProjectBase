namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public enum RoomType
    {
        Side,
        Horizontal,
        Down,
        Arrival,
    }

    [Flags]
    public enum RoomOpening
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8,
    }

    [Serializable]
    public struct RoomPassage
    {
        public RoomOpening entry;
        public RoomOpening exit;
    }

    [Serializable]
    public sealed class RoomTerrainPattern
    {
        public string id;
        public GameObject prefab;
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

    /// <summary>방 하나의 제작 원본. 통로 조합과 지형은 각각 제작자가 지정한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grid))]
    public sealed class RoomTemplate : MonoBehaviour
    {
        public const int RoomSize = 20;
        public const int OpeningSize = 4;
        public const int HorizontalOpeningStart = 4;
        public const int VerticalOpeningStart = 13;

        [SerializeField] private string _templateId;
        [Tooltip("같은 지형 구조의 Variant가 공유하는 Base 식별자입니다. 비어 있으면 템플릿 ID를 사용합니다.")]
        [SerializeField] private string _baseGroupId;
        [SerializeField] private RoomType _roomType;
        [SerializeField] private RoomOpening _openings;
        [SerializeField] private Tilemap _fixedGeometry;
        [Tooltip("후보 통로. 실제 Player로 모든 통과 방향을 확인하기 전에는 검증 완료로 표시하지 않습니다.")]
        [SerializeField] private List<RoomPassage> _passages = new();
        [Tooltip("기록된 모든 통과 방향을 실제 Player로 확인한 경우에만 활성화합니다.")]
        [SerializeField] private bool _passagesVerified;
        [SerializeField] private List<RectInt> _variationRegions = new();
        [SerializeField] private List<RectInt> _protectedRegions = new();
        [SerializeField] private List<RoomTerrainVariation> _terrainVariations = new();
        [SerializeField] private List<Vector2Int> _contentSlots = new();
        [SerializeField] private List<RoomContentSlot> _contentSettings = new();
        [SerializeField] private Transform _playerStart;
        [SerializeField] private Transform _goal;
        [Tooltip("Player 시작 마커에서 출입구까지 실제 이동으로 확인한 방향입니다.")]
        [SerializeField] private RoomOpening _verifiedStartExits;
        [Tooltip("출입구에서 Goal 마커까지 실제 이동으로 확인한 방향입니다.")]
        [SerializeField] private RoomOpening _verifiedGoalEntries;
        [Tooltip("막다른 방의 해당 출입구에서 들어와 내부를 방문한 뒤 돌아오는 이동을 확인한 방향입니다.")]
        [SerializeField] private RoomOpening _verifiedDeadEndReturns;
        [SerializeField] private Transform _optionalTarget;

        public string TemplateId => _templateId;
        public string BaseGroupId => string.IsNullOrWhiteSpace(_baseGroupId) ? _templateId : _baseGroupId;
        public RoomType Type => _roomType;
        // Unity의 Flags Inspector는 Everything을 -1로 직렬화한다.
        public RoomOpening Openings => _openings == (RoomOpening)(-1)
            ? RoomOpening.North | RoomOpening.East | RoomOpening.South | RoomOpening.West
            : _openings;
        public Tilemap FixedGeometry => _fixedGeometry;
        public IReadOnlyList<RoomPassage> Passages => _passages;
        public bool PassagesVerified => _passagesVerified;
        public IReadOnlyList<RectInt> VariationRegions => _variationRegions;
        public IReadOnlyList<RectInt> ProtectedRegions => _protectedRegions;
        public IReadOnlyList<RoomTerrainVariation> TerrainVariations => _terrainVariations;
        public IReadOnlyList<Vector2Int> ContentSlots => _contentSlots;
        public IReadOnlyList<RoomContentSlot> ContentSettings => _contentSettings;
        public Transform PlayerStart => _playerStart;
        public Transform Goal => _goal;
        public RoomOpening VerifiedStartExits => _verifiedStartExits;
        public RoomOpening VerifiedGoalEntries => _verifiedGoalEntries;
        public RoomOpening VerifiedDeadEndReturns => _verifiedDeadEndReturns;
        public Transform OptionalTarget => _optionalTarget;

        /// <summary>방 내부 좌표의 경계 셀. 문 폭과 위치의 단일 기준이다.</summary>
        public static RectInt OpeningCells(RoomOpening opening)
        {
            switch (opening)
            {
                case RoomOpening.North: return new RectInt(VerticalOpeningStart, RoomSize - 1, OpeningSize, 1);
                case RoomOpening.East: return new RectInt(RoomSize - 1, HorizontalOpeningStart, 1, OpeningSize);
                case RoomOpening.South: return new RectInt(VerticalOpeningStart, 0, OpeningSize, 1);
                case RoomOpening.West: return new RectInt(0, HorizontalOpeningStart, 1, OpeningSize);
                default: throw new ArgumentOutOfRangeException(nameof(opening));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(new Vector3(RoomSize / 2f, RoomSize / 2f), new Vector3(RoomSize, RoomSize));

            Gizmos.color = Color.green;
            foreach (RoomOpening side in new[] { RoomOpening.North, RoomOpening.East, RoomOpening.South, RoomOpening.West })
            {
                if ((_openings & side) == 0) continue;
                RectInt rect = OpeningCells(side);
                Gizmos.DrawWireCube(new Vector3(rect.center.x, rect.center.y), new Vector3(rect.width, rect.height));
            }

            Gizmos.color = Color.cyan;
            foreach (RectInt region in _variationRegions)
                Gizmos.DrawWireCube(new Vector3(region.center.x, region.center.y), new Vector3(region.width, region.height));

            Gizmos.color = Color.red;
            foreach (RectInt region in _protectedRegions)
                Gizmos.DrawWireCube(new Vector3(region.center.x, region.center.y), new Vector3(region.width, region.height));

            Gizmos.color = Color.yellow;
            foreach (Vector2Int point in _contentSlots)
                Gizmos.DrawWireSphere(new Vector3(point.x + 0.5f, point.y + 0.5f), 0.25f);
        }
    }
}
