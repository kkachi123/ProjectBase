using UnityEngine;
using GridMapSystem;

[System.Serializable]
public class GridSpawnPrefabSet
{
    public GameObject coinPrefab;
    public GameObject monsterPrefab;
    public GameObject itemPrefab;
    public GameObject arrowTrapPrefab;
    public GameObject spikeTrapPrefab;

    public GameObject Get(SpawnKind kind)
    {
        switch (kind)
        {
            case SpawnKind.Coin:
                return coinPrefab;
            case SpawnKind.Monster:
                return monsterPrefab;
            case SpawnKind.Item:
                return itemPrefab;
            case SpawnKind.ArrowTrap:
                return arrowTrapPrefab;
            case SpawnKind.SpikeTrap:
                return spikeTrapPrefab;
            default:
                return null;
        }
    }
}
