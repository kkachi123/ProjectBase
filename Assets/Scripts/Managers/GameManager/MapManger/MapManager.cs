using UnityEngine;
using GridMapSystem;

public class MapManager : MonoBehaviour
{
    [SerializeField] private GridMapGenerationSettings _generationSettings;
    [SerializeField] private bool _generateOnStart;

    public GeneratedMapInfo CurrentMapInfo { get; private set; }
    public GridMapGenerationSettings GenerationSettings => _generationSettings;

    private void Start()
    {
        if (_generateOnStart)
            GenerateMap();
    }

    public GeneratedMapInfo GenerateMap()
    {
        CurrentMapInfo = GridChunkRuntimeGenerator.Generate(_generationSettings);
        return CurrentMapInfo;
    }

    public void ClearMap()
    {
        GridChunkRuntimeGenerator.ClearGeneratedMap();
        CurrentMapInfo = null;
    }
}
