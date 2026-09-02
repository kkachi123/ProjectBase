using UnityEngine;
public class TitleScreen : UIScreen
{
    public void OnClickStartButton()
    {
        Managers.Instance.SceneFlow.StartGame();
    }

    public void OnClickQuitButton()
    {
        Application.Quit();
    }
}
