using UnityEngine;
using GridMapSystem;

[CreateAssetMenu(fileName = "GridMapGenerationSettings", menuName = "Map/Grid Map Generation Settings")]
public class GridMapGenerationSettings : ScriptableObject
{
    public GridChunkDatabase database;
    public int contentChunkCount = 10;
    public int maxThreeDirCount = 2;
    public bool useRandomSeed = true;
    public int randomSeed;
    public GridSpawnPrefabSet spawnPrefabs = new GridSpawnPrefabSet();

    public int ResolveSeed()
    {
        return useRandomSeed ? System.Environment.TickCount : randomSeed;
    }
}
