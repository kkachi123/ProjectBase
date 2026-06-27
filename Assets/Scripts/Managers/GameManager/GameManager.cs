using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, GameOver }

    public GameState State { get; private set; } = GameState.Playing;

    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        Managers.Instance.UI.Overlay.ShowPauseDim(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        Managers.Instance.UI.Overlay.ShowPauseDim(false);
    }

    public void TriggerGameOver()
    {
        State = GameState.GameOver;
        Time.timeScale = 0f;
    }
}
