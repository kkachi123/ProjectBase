using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIHUD _hud;
    [SerializeField] private UIOverlayController _overlay;
    [SerializeField] private UIDialogueController _dialogue;
    [SerializeField] private UINotificationController _notification;
    //private readonly UIScreenNavigator _navigator = new();
    [SerializeField] private UIMenuTabController _menuTabController;

    public UIHUD HUD => _hud;
    public UIOverlayController Overlay => _overlay;
    public UIDialogueController Dialogue => _dialogue;
    public UINotificationController Notification => _notification;
    public UIMenuTabController MenuTabController => _menuTabController;

    void Start()
    {
        _menuTabController.Initialize();
    }
}
