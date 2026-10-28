namespace ProjectRE
{
using System;
using System.Collections;
using UnityEngine;

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

    // 맵·Player 생성 후 최초 상태 진입을 확인하고 플레이 시작.
    public IEnumerator StartRun()
    {
        if (State == RunState.Starting || State == RunState.Playing) yield break;

        State = RunState.Starting;

        if (Managers.Instance.Map == null)
        {
            FailStartRun("MapManager is missing.");
            yield break;
        }
        // Map, Player 생성. 
        CurrentMapInfo = Managers.Instance.Map.GenerateMap();

        if (CurrentMapInfo == null)
        {
            FailStartRun("Failed to generate map.", clearMap: false);
            yield break;
        }

        if (_playerSpawnManager == null)
        {
            FailStartRun("PlayerSpawnManager is missing.");
            yield break;
        }

        PlayerController player = _playerSpawnManager.Spawn(CurrentMapInfo.playerSpawnPosition);
        if (player == null)
        {
            FailStartRun("Failed to spawn Player.");
            yield break;
        }

        // 생성 프레임의 Awake 이후 다음 프레임에서 Start 완료 확인.
        yield return null;
        if (player == null || !player.isActiveAndEnabled || !player.IsReady)
        {
            FailStartRun("Player initialization did not complete.");
            yield break;
        }

        _playerSpawnManager.RegisterCurrentPlayer();
        _notifyDialogue?.RegisterDialogue(Managers.Instance.UI.InGameUI?.Dialogue);
        Managers.Instance.Camera?.BindTarget(player.transform);
        PrepareLighting();

        Managers.Instance.Game.Reset();
        State = RunState.Playing;
    }

    // 시작 실패 시 부분 생성물 정리. Title 복귀는 현재 씬 전환에서 처리.
    private void FailStartRun(string message, bool clearMap = true)
    {
        Debug.LogError($"[AdventureRunManager] {message}");
        if (clearMap) ClearRunObjects();
        State = RunState.Failed;
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
}
