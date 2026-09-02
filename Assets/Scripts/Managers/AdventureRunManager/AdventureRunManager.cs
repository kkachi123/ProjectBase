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
        // BindCamera(player); //추가 예정.
        // BindHUD(player); //추가 예정.
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
        State = RunState.Completed;
    }

    public void FailRun()
    {
        State = RunState.Failed;
    }

    public void ClearRun()
    {
        _playerSpawnManager?.ClearCurrentPlayer();
        Managers.Instance.Map?.ClearMap();
        CurrentMapInfo = null;
        State = RunState.None;
    }
}
