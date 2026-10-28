namespace ProjectRE
{
using UnityEngine;
using ProjectRE.MapGeneration;

public class MapManager : MonoBehaviour
{
    [SerializeField] private MapPlanningSettings _planningSettings;
    [SerializeField] private GameObject _goalPrefab;
    [SerializeField] private bool _generateOnStart;
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _seed;

    public GeneratedMapInfo CurrentMapInfo { get; private set; }

    private void Start()
    {
        if (_generateOnStart)
            GenerateMap();
    }

    public GeneratedMapInfo GenerateMap()
    {
        int seed = _useRandomSeed ? System.Environment.TickCount : _seed;
        var context = new MapBuildContext();
        if (!RoomMapFactory.TryCreate(_planningSettings, seed, _goalPrefab, context, out var next, out _, out string error))
        {
            Debug.LogError($"[MapManager] {error}");
            return null;
        }

        GeneratedMapInfo previous = CurrentMapInfo;
        try
        {
            if (previous != null) previous.gameObject.SetActive(false);
            next.gameObject.SetActive(true);
            CurrentMapInfo = next;
        }
        catch (System.Exception exception)
        {
            context.DestroyObject(next.gameObject);
            if (previous != null) previous.gameObject.SetActive(true);
            Debug.LogError($"[MapManager] 맵 활성화 실패: {exception.Message}");
            return null;
        }
        if (previous != null) context.DestroyObject(previous.gameObject);
        return CurrentMapInfo;
    }

    public void ClearMap()
    {
        if (CurrentMapInfo != null)
        {
            CurrentMapInfo.gameObject.SetActive(false);
            new MapBuildContext().DestroyObject(CurrentMapInfo.gameObject);
        }
        CurrentMapInfo = null;
    }
}
}
