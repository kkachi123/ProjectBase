using UnityEngine;
using GridMapSystem;

public class AdventureRunManager : MonoBehaviour
{
    public enum RunState
    {
        None,
        Starting,
        Playing,
        Completed,
        Failed,
    }

    public RunState State { get; private set; } = RunState.None;
    public GeneratedMapInfo CurrentMapInfo { get; private set; }

    public void StartRun()
    {
        if (State == RunState.Starting) return;

        State = RunState.Starting;

        if (Managers.Instance.Map == null)
        {
            Debug.LogError("[AdventureRunManager] MapManager is missing.");
            State = RunState.Failed;
            return;
        }

        Managers.Instance.Game.Reset();
        CurrentMapInfo = Managers.Instance.Map.GenerateMap();

        if (CurrentMapInfo == null)
        {
            Debug.LogError("[AdventureRunManager] Failed to generate map.");
            State = RunState.Failed;
            return;
        }

        State = RunState.Playing;
    }

    public void EndRun()
    {
        State = RunState.Completed;
    }

    public void FailRun()
    {
        State = RunState.Failed;
    }

    public void ClearRun()
    {
        Managers.Instance.Map?.ClearMap();
        CurrentMapInfo = null;
        State = RunState.None;
    }
}
