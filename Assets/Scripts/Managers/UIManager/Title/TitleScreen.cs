using UnityEngine;
public class TitleScreen : UIScreen
{
    public void OnClickStartButton()
    {
        Managers.Instance.Flow.StartGame();
    }

    public void OnClickQuitButton()
    {
        Application.Quit();
    }
}
