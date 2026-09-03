using UnityEngine;
using GridMapSystem;

public class AdventureRunManager : MonoBehaviour
{
    [SerializeField] private PlayerSpawnManager _playerSpawnManager;

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

        if (_playerSpawnManager == null)
        {
            Debug.LogError("[AdventureRunManager] PlayerSpawnManager is missing.");
            State = RunState.Failed;
            return;
        }

        PlayerController player = _playerSpawnManager.Spawn(CurrentMapInfo.playerSpawnPosition);
        if (player == null)
        {
            State = RunState.Failed;
            return;
        }

        Managers.Instance.Camera?.BindTarget(player.transform);
        PrepareLighting();

        State = RunState.Playing;
    }

    private void PrepareLighting()
    {
        LightingManager lighting = Managers.Instance.Lighting;
        if (lighting == null)
            lighting = FindFirstObjectByType<LightingManager>();

        if (lighting == null)
            lighting = new GameObject("LightingManager").AddComponent<LightingManager>();

        lighting.PrepareRunLighting();
    }

    public void EndRun()
    {
        if (State != RunState.Playing) return;

        State = RunState.Completed;
        ClearRunObjects();
        Managers.Instance.SceneFlow.GoToTitle();
    }

    public void FailRun()
    {
        if (State != RunState.Playing) return;

        State = RunState.Failed;
        ClearRunObjects();
        Managers.Instance.Game.TriggerGameOver();
        Managers.Instance.SceneFlow.GoToTitle();
    }

    public void ClearRun()
    {
        ClearRunObjects();
        State = RunState.None;
    }

    private void ClearRunObjects()
    {
        _playerSpawnManager?.ClearCurrentPlayer();
        Managers.Instance.Map?.ClearMap();
        CurrentMapInfo = null;
    }
}
