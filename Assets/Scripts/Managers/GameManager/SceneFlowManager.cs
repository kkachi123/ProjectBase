namespace ProjectRE
{
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public enum SceneFlowState { Title, InGame, Loading }

    [SerializeField] private string _titleScene = "Title";
    [SerializeField] private string _gameScene = "Game";
    [SerializeField] private float _fadeTime = 0.5f;

    public SceneFlowState State { get; private set; } = SceneFlowState.Title;

    public void StartGame() => StartCoroutine(TransitionTo(_gameScene, SceneFlowState.InGame));
    public void RestartGame() => StartCoroutine(TransitionTo(_gameScene, SceneFlowState.InGame));
    public void GoToTitle() => StartCoroutine(TransitionTo(_titleScene, SceneFlowState.Title));

    private IEnumerator TransitionTo(string sceneName, SceneFlowState nextState)
    {
        if (State == SceneFlowState.Loading) yield break;
        State = SceneFlowState.Loading;

        UIOverlayController overlay = Managers.Instance.UI.Overlay;
        overlay.FadeIn(_fadeTime);
        overlay.ShowLoading(true);
        yield return new WaitForSecondsRealtime(_fadeTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            overlay.Loading.SetProgress(Mathf.Clamp01(op.progress / 0.9f));
            yield return null;
        }

        overlay.Loading.SetProgress(1f);
        Time.timeScale = 1f;
        if (nextState == SceneFlowState.InGame)
        {
            AdventureRunManager run = Managers.Instance.AdventureRun;
            if (run != null)
                yield return run.StartRun();

            if (run == null || run.State != AdventureRunManager.RunState.Playing)
            {
                if (run == null)
                    Debug.LogError("[SceneFlowManager] AdventureRunManager is missing.");
                // Loading 재진입 없이 현재 전환에서 실패 복귀 완료.
                yield return SceneManager.LoadSceneAsync(_titleScene);
                nextState = SceneFlowState.Title;
            }
        }
        State = nextState;
        overlay.ShowLoading(false);
        overlay.FadeOut(_fadeTime);
    }
}
}
