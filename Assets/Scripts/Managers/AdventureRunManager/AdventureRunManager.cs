using System;
using UnityEngine;
using GridMapSystem;

public class AdventureRunManager : MonoBehaviour
{
    [SerializeField] private PlayerSpawnManager _playerSpawnManager;
    [SerializeField] private NotifyDialogue _notifyDialogue;

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
        // Map, Player 생성. 
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

        _notifyDialogue?.RegisterDialogue(Managers.Instance.UI.InGameUI?.Dialogue);
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
        Func<Action, bool> action = _notifyDialogue != null ? _notifyDialogue.PlayClear : null;
        FinishRun(RunState.Completed, null, action);
    }

    public void FailRun()
    {
        Func<Action, bool> action = _notifyDialogue != null ? _notifyDialogue.PlayGameOver : null;
        FinishRun(RunState.Failed, Managers.Instance.Game.TriggerGameOver, action);
    }

    private void FinishRun(RunState resultState, Action beforeMessage, Func<Action, bool> playMessage)
    {
        if (State != RunState.Playing) return;

        State = resultState;
        beforeMessage?.Invoke();

        // 메시지 재생 후, 타이틀로 돌아가기
        if (playMessage != null && playMessage(ReturnToTitleAfterRun))
            return;

        // 메시지 재생이 없을 시 바로 타이틀로 돌아가기
        ReturnToTitleAfterRun();
    }

    public void ClearRun()
    {
        ClearRunObjects();
        State = RunState.None;
    }

    private void ClearRunObjects()
    {
        _notifyDialogue?.UnregisterDialogue();
        _playerSpawnManager?.ClearCurrentPlayer();
        Managers.Instance.Map?.ClearMap();
        CurrentMapInfo = null;
    }

    private void ReturnToTitleAfterRun()
    {
        ClearRunObjects();
        Managers.Instance.SceneFlow.GoToTitle();
    }
}
