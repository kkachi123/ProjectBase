using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public enum FlowState { Title, InGame, Loading }

    [SerializeField] private string _titleScene = "Title";
    [SerializeField] private string _gameScene = "Game";
    [SerializeField] private float _fadeTime = 0.5f;

    public FlowState State { get; private set; } = FlowState.Title;

    public void StartGame() => StartCoroutine(TransitionTo(_gameScene, FlowState.InGame));
    public void RestartGame() => StartCoroutine(TransitionTo(_gameScene, FlowState.InGame));
    public void GoToTitle() => StartCoroutine(TransitionTo(_titleScene, FlowState.Title));

    private IEnumerator TransitionTo(string sceneName, FlowState nextState)
    {
        if (State == FlowState.Loading) yield break;
        State = FlowState.Loading;

        UIOverlayController overlay = Managers.Instance.UI.Overlay;
        overlay.FadeIn(_fadeTime);
        overlay.ShowLoading(true);
        yield return new WaitForSecondsRealtime(_fadeTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        yield return op;

        Time.timeScale = 1f;
        if (nextState == FlowState.InGame)
            Managers.Instance.Game.Reset();
        State = nextState;
        overlay.ShowLoading(false);
        overlay.FadeOut(_fadeTime);
    }
}
