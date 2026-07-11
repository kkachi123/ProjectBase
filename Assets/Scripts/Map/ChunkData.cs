using UnityEngine;
using System.Collections.Generic;

public enum ChunkType
{
    Transition,
    Combat,
    Puzzle,
    Treasure,
}

public class ChunkData : MonoBehaviour
{
    [SerializeField] private ChunkType chunkType;
    [SerializeField] private Vector2Int entrance1 = new Vector2Int(0, 3);
    [SerializeField] private Vector2Int entrance2 = new Vector2Int(20, 3);
    [SerializeField] private List<Vector2Int> coins;
    [SerializeField] private List<Vector2Int> monsters;
    [SerializeField] private List<Vector2Int> items;
    [SerializeField] private List<Vector2Int> arrowTraps;
    [SerializeField] private List<Vector2Int> spikeTraps;
    
    // Editor Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(this.transform.position + new Vector3(entrance1.x, entrance1.y), 0.5f);
        Gizmos.DrawSphere(this.transform.position + new Vector3(entrance2.x, entrance2.y), 0.5f);
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
        if(chunkType == ChunkType.Transition)
        {
            width = 10;
        }

        // Draw Width Height Cube
        Gizmos.DrawWireCube(this.transform.position + new Vector3(width / 2f, height / 2f), new Vector3(width, height));
    }
}
