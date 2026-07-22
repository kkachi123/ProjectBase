using UnityEngine;
using System.Collections.Generic;

public enum ChunkType
{
    Transition,
    Combat,
    Puzzle,
    Treasure,
    EndLine, // 막다른길(단일 진입점) 전용 분류. Start/End(시작/목표) 청크도 이 타입을 사용한다.
}

// ChunkType.EndLine 청크의 세부 역할. 일반 막다른길(Normal)과 구분해서
// 생성기가 맵의 시작/최종 목표 지점을 특정해서 배치할 수 있도록 한다.
public enum EndLineRole
{
    Normal,
    Start,
    End,
}

// 격자 기반 조합을 위한 연결 지점. 방향(4면) x 슬롯(면당 2개)으로 고정된 8개 지점 중에서만
// 고를 수 있다 — 임의 좌표 입력을 막아서, 서로 다른 청크라도 같은 슬롯이면 물리적으로도
// 문 위치가 정확히 맞물리게 한다(실제 좌표는 EntranceSlotResolver가 계산).
[System.Flags]
public enum EntranceSlot
{
    None = 0,
    NorthLeft = 1 << 0,
    NorthRight = 1 << 1,
    SouthLeft = 1 << 2,
    SouthRight = 1 << 3,
    EastLow = 1 << 4,
    EastHigh = 1 << 5,
    WestLow = 1 << 6,
    WestHigh = 1 << 7,
}

// EntranceSlot 하나를 실제 청크 로컬 좌표(픽셀)로 변환한다. North/South는 위/아래 변,
// East/West는 좌/우 변이고, Left·Right(가로 위치)와 Low·High(세로 위치)는 각각 변 위의
// 고정된 두 지점이다 — 모든 청크가 이 계산식을 공유해야 슬롯이 일치할 때 문도 실제로 이어진다.
public static class EntranceSlotResolver
{
    public static Vector2Int Resolve(EntranceSlot slot, int width, int height)
    {
        switch (slot)
        {
            case EntranceSlot.WestLow: return new Vector2Int(0, height * 3 / 10);
            case EntranceSlot.WestHigh: return new Vector2Int(0, height * 7 / 10);
            case EntranceSlot.EastLow: return new Vector2Int(width, height * 3 / 10);
            case EntranceSlot.EastHigh: return new Vector2Int(width, height * 7 / 10);
            case EntranceSlot.SouthLeft: return new Vector2Int(width / 4, 0);
            case EntranceSlot.SouthRight: return new Vector2Int(width * 3 / 4, 0);
            case EntranceSlot.NorthLeft: return new Vector2Int(width / 4, height);
            case EntranceSlot.NorthRight: return new Vector2Int(width * 3 / 4, height);
            default: return Vector2Int.zero;
        }
    }

    // entranceSlots 플래그에 켜져 있는 슬롯들을 전부 좌표로 변환한다.
    public static List<Vector2Int> ResolveAll(EntranceSlot slots, int width, int height)
    {
        var result = new List<Vector2Int>();
        foreach (EntranceSlot slot in System.Enum.GetValues(typeof(EntranceSlot)))
        {
            if (slot == EntranceSlot.None) continue;
            if ((slots & slot) == slot) result.Add(Resolve(slot, width, height));
        }
        return result;
    }
}

public class ChunkData : MonoBehaviour
{
    [SerializeField] private ChunkType chunkType;
    // 이 청크가 실제로 연결 가능한 지점들. 4방향 x 슬롯 2개(총 8개) 중 체크된 것만 사용된다.
    // 1개면 막다른길(입구=출구인 단일 진입점), 2개 이상이면 연결 시 그중 하나를 입구로 삼고
    // 나머지 중 하나를 출구로 무작위/조건에 맞게 골라 쓴다(실제 선택은 생성기 쪽
    // GetAllEntrances() 소비 로직에서 이뤄진다).
    [SerializeField] private EntranceSlot entranceSlots = EntranceSlot.WestLow | EntranceSlot.EastLow;
    // chunkType == EndLine 일 때만 의미 있음. 일반 막다른길인지, 맵의 시작/끝인지 구분.
    [SerializeField] private EndLineRole endLineRole = EndLineRole.Normal;
    [SerializeField] private List<Vector2Int> coins;
    [SerializeField] private List<Vector2Int> monsters;
    [SerializeField] private List<Vector2Int> items;
    [SerializeField] private List<Vector2Int> arrowTraps;
    [SerializeField] private List<Vector2Int> spikeTraps;

    public ChunkType ChunkType => chunkType;
    public EndLineRole EndLineRole => endLineRole;
    public EntranceSlot EntranceSlots => entranceSlots;

    private int Width => (chunkType == ChunkType.Transition || chunkType == ChunkType.EndLine) ? 10 : 20;
    private const int Height = 10;

    public List<Vector2Int> GetAllEntrances()
    {
        return EntranceSlotResolver.ResolveAll(entranceSlots, Width, Height);
    }

    public List<Vector2Int> GetCoins() => coins != null ? new List<Vector2Int>(coins) : new List<Vector2Int>();
    public List<Vector2Int> GetMonsters() => monsters != null ? new List<Vector2Int>(monsters) : new List<Vector2Int>();
    public List<Vector2Int> GetItems() => items != null ? new List<Vector2Int>(items) : new List<Vector2Int>();
    public List<Vector2Int> GetArrowTraps() => arrowTraps != null ? new List<Vector2Int>(arrowTraps) : new List<Vector2Int>();
    public List<Vector2Int> GetSpikeTraps() => spikeTraps != null ? new List<Vector2Int>(spikeTraps) : new List<Vector2Int>();

    // Editor Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var entrance in GetAllEntrances())
        {
            Gizmos.DrawSphere(this.transform.position + new Vector3(entrance.x, entrance.y), 0.5f);
        }
        Gizmos.color = Color.yellow;
        foreach (var coin in coins)
        {
            Gizmos.DrawSphere(this.transform.position + new Vector3(coin.x, coin.y), 0.2f);
        }
        Gizmos.color = Color.red;
        foreach (var monster in monsters)
        {
            Gizmos.DrawSphere(this.transform.position + new Vector3(monster.x, monster.y), 0.5f);
        }
        Gizmos.color = Color.blue;
        foreach (var item in items)
        {
            Gizmos.DrawSphere(this.transform.position + new Vector3(item.x, item.y), 0.25f);
        }
        Gizmos.color = Color.magenta;
        foreach (var arrowTrap in arrowTraps)
        {
            Gizmos.DrawLine(this.transform.position + new Vector3(arrowTrap.x + 0.5f, arrowTrap.y), this.transform.position + new Vector3(arrowTrap.x + 0.5f, arrowTrap.y + 1f));
            // 4pixel
            Gizmos.DrawCube(this.transform.position + new Vector3(arrowTrap.x + 0.5f + 4f, arrowTrap.y + 0.5f), Vector3.one);
        }
        Gizmos.color = Color.cyan;
        foreach (var spikeTrap in spikeTraps)
        {
            Gizmos.DrawCube(this.transform.position + new Vector3(spikeTrap.x , spikeTrap.y + 0.5f), Vector3.one + 2f * new Vector3(0.5f, 0));
        }

        // 20x20 grid
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(this.transform.position + new Vector3(Width / 2f, Height / 2f), new Vector3(Width, Height));
    }
}
