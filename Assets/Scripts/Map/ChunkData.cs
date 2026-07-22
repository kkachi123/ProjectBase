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

public class ChunkData : MonoBehaviour
{
    [SerializeField] private ChunkType chunkType;
    // 이 청크가 실제로 연결 가능한 모든 지점. 1개면 막다른길(입구=출구인 단일 진입점),
    // 2개 이상이면 연결 시 그중 하나를 입구로 삼고 나머지 중 하나를 출구로 무작위/조건에
    // 맞게 골라 쓴다(실제 선택은 생성기 쪽 GetAllEntrances() 소비 로직에서 이뤄진다).
    [SerializeField] private List<Vector2Int> entrances = new List<Vector2Int> { new Vector2Int(0, 3), new Vector2Int(20, 3) };
    // chunkType == EndLine 일 때만 의미 있음. 일반 막다른길인지, 맵의 시작/끝인지 구분.
    [SerializeField] private EndLineRole endLineRole = EndLineRole.Normal;
    [SerializeField] private List<Vector2Int> coins;
    [SerializeField] private List<Vector2Int> monsters;
    [SerializeField] private List<Vector2Int> items;
    [SerializeField] private List<Vector2Int> arrowTraps;
    [SerializeField] private List<Vector2Int> spikeTraps;

    public List<Vector2Int> GetAllEntrances()
    {
        return entrances != null ? new List<Vector2Int>(entrances) : new List<Vector2Int>();
    }

    // Editor Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var entrance in entrances)
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
        int width = 20;
        int height = 10;
        if(chunkType == ChunkType.Transition || chunkType == ChunkType.EndLine)
        {
            width = 10;
        }

        // Draw Width Height Cube
        Gizmos.DrawWireCube(this.transform.position + new Vector3(width / 2f, height / 2f), new Vector3(width, height));
    }
}
