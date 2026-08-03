using UnityEngine;
using System.Collections.Generic;

namespace GridMapSystem
{
    public enum GridChunkType
    {
        Transition,
        Content, // 전투/퍼즐/보물 등 세부 구분 없이 하나로 통합된 콘텐츠 청크. 구조만 다르게
                 // 만들고, 실제 스폰되는 몬스터/함정/코인 등은 스폰 포인트 + 난이도별 확률로 정한다.
        EndLine, // 막다른길(단일 진입점) 전용 분류. Start/End(시작/목표) 청크도 이 타입을 사용한다.
    }

    // GridChunkType.EndLine 청크의 세부 역할. 일반 막다른길(Normal)과 구분해서
    // 생성기가 맵의 시작/최종 목표 지점을 특정해서 배치할 수 있도록 한다.
    public enum GridEndLineRole
    {
        Normal,
        Start,
        End,
    }

    // 격자 기반 조합을 위한 연결 지점. 방향(4면)당 고정된 지점 1개씩만 쓴다 — 임의 좌표
    // 입력을 막아서, 서로 다른 청크라도 같은 방향이면 물리적으로도 문 위치가 정확히
    // 맞물리게 한다(실제 좌표는 GridEntranceSlotResolver가 계산).
    // 원래는 방향당 슬롯 2개(Low/High, Left/Right)였는데 제작 시간상 방향당 1개로 축소했다.
    // 남긴 슬롯의 비트값은 기존(Low/Left 계열)과 동일하게 유지 — 이미 만든 프리팹 데이터가
    // 그대로 유효하다.
    [System.Flags]
    public enum GridEntranceSlot
    {
        None = 0,
        North = 1 << 0,  // 기존 NorthLeft
        South = 1 << 3,  // 기존 SouthRight
        East = 1 << 4,   // 기존 EastLow
        West = 1 << 6,   // 기존 WestLow
    }

    // 스폰 포인트 하나에 실제로 무엇을 놓을지는 저장하지 않는다 — 좌표만 authoring해두고,
    // 실제 종류는 호출 시점에 무작위로 정한다(난이도별 확률 조정은 이후 스포너 쪽에서 처리).
    public enum SpawnKind
    {
        Coin,
        Monster,
        Item,
        ArrowTrap,
        SpikeTrap,
    }

    // GridChunkData.GetResolvedSpawns()가 사용하는 리졸버. GridEntranceSlotResolver와 같은
    // 패턴 — "무엇을 어디에 배치할지"를 정하는 계산을 데이터 클래스 밖으로 분리해둔다.
    public static class GridSpawnResolver
    {
        public static List<(Vector2Int position, SpawnKind kind)> ResolveAll(List<Vector2Int> points, System.Random rng)
        {
            var result = new List<(Vector2Int, SpawnKind)>();
            SpawnKind[] kinds = (SpawnKind[])System.Enum.GetValues(typeof(SpawnKind));
            foreach (var p in points)
                result.Add((p, kinds[rng.Next(kinds.Length)]));
            return result;
        }
    }

    // GridEntranceSlot 하나를 실제 청크 로컬 좌표(픽셀)로 변환한다. North/South는 위/아래 변,
    // East/West는 좌/우 변의 고정된 지점이다 — 모든 청크가 이 계산식을 공유해야 방향이
    // 일치할 때 문도 실제로 이어진다.
    public static class GridEntranceSlotResolver
    {
        public static Vector2Int Resolve(GridEntranceSlot slot, int width, int height)
        {
            switch (slot)
            {
                case GridEntranceSlot.West: return new Vector2Int(0, height * 3 / 10);
                case GridEntranceSlot.East: return new Vector2Int(width, height * 3 / 10);
                case GridEntranceSlot.South: return new Vector2Int(width * 3 / 4, 0);
                case GridEntranceSlot.North: return new Vector2Int(width / 4, height);
                default: return Vector2Int.zero;
            }
        }

        // entranceSlots 플래그에 켜져 있는 슬롯들을 전부 좌표로 변환한다.
        public static List<Vector2Int> ResolveAll(GridEntranceSlot slots, int width, int height)
        {
            var result = new List<Vector2Int>();
            foreach (GridEntranceSlot slot in System.Enum.GetValues(typeof(GridEntranceSlot)))
            {
                if (slot == GridEntranceSlot.None) continue;
                if ((slots & slot) == slot) result.Add(Resolve(slot, width, height));
            }
            return result;
        }
    }

    // 그리드 기반 조합기(방식 2) 전용 청크 데이터. 기존 ChunkData(방식 1, 픽셀 커서 기반)와는
    // 완전히 독립된 컴포넌트다 — 같은 프리팹에 둘 다 붙여서 병행 실험할 수 있다.
    public class GridChunkData : MonoBehaviour
    {
        [SerializeField] private GridChunkType chunkType;
        // 이 청크가 실제로 연결 가능한 지점들. 4방향 중 체크된 것만 사용된다.
        // 1개면 막다른길(입구=출구인 단일 진입점), 2개 이상이면 연결 시 그중 하나를 입구로 삼고
        // 나머지 중 하나를 출구로 무작위/조건에 맞게 골라 쓴다.
        [SerializeField] private GridEntranceSlot entranceSlots = GridEntranceSlot.West | GridEntranceSlot.East;
        // chunkType == EndLine 일 때만 의미 있음. 일반 막다른길인지, 맵의 시작/끝인지 구분.
        [SerializeField] private GridEndLineRole endLineRole = GridEndLineRole.Normal;
        // 코인/몬스터/아이템/함정 구분 없이 좌표만 authoring한다. 실제 종류는
        // GetResolvedSpawns()가 호출 시점에 무작위로 정한다.
        [SerializeField] private List<Vector2Int> spawnPoints;

        public GridChunkType ChunkType => chunkType;
        public GridEndLineRole EndLineRole => endLineRole;
        public GridEntranceSlot EntranceSlots => entranceSlots;

        // Transition/EndLine = 10x10(1칸), Content = 20x20(2x2칸). 전부 10의 배수라 리사이즈
        // 없이 칸수로 환산 가능하다.
        public const int CellSize = 10;
        private bool IsSmall => chunkType == GridChunkType.Transition || chunkType == GridChunkType.EndLine;
        private int Width => IsSmall ? 10 : 20;
        private int Height => IsSmall ? 10 : 20;
        public Vector2Int Footprint => new Vector2Int(Width / CellSize, Height / CellSize);

        public List<Vector2Int> GetAllEntrances()
        {
            return GridEntranceSlotResolver.ResolveAll(entranceSlots, Width, Height);
        }

        public List<Vector2Int> GetSpawnPoints() => spawnPoints != null ? new List<Vector2Int>(spawnPoints) : new List<Vector2Int>();

        // 스폰 포인트마다 실제 종류(코인/몬스터/아이템/함정)를 무작위로 배정해서 반환한다.
        // 호출할 때마다(다른 rng 상태로) 다시 부르면 다른 결과가 나온다 — 결과를 저장해두지 않는다.
        public List<(Vector2Int position, SpawnKind kind)> GetResolvedSpawns(System.Random rng)
        {
            return GridSpawnResolver.ResolveAll(GetSpawnPoints(), rng);
        }

        // Editor Gizmo
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (var entrance in GetAllEntrances())
            {
                Gizmos.DrawSphere(this.transform.position + new Vector3(entrance.x, entrance.y), 0.5f);
            }
            // 실제 종류는 호출 시점에 무작위로 정해지므로, 에디터에서는 전부 같은 표시로만 그린다.
            Gizmos.color = Color.yellow;
            foreach (var p in GetSpawnPoints())
            {
                Gizmos.DrawSphere(this.transform.position + new Vector3(p.x, p.y), 0.3f);
            }

            // 격자 칸 경계 표시(셀 크기 단위)
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(this.transform.position + new Vector3(Width / 2f, Height / 2f), new Vector3(Width, Height));
        }
    }
}
