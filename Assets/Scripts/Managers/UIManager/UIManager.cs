using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Overlay만 DontDestroyOnLoad 오브젝트의 자식으로 상주 → SerializeField 유지
    [SerializeField] private UIOverlayController _overlay;

    // 씬별 UI는 각자 Awake에서 등록
    private UIHUD _hud;
    private UIDialogueController _dialogue;
    private UINotificationController _notification;
    private UIMenuTabController _menuTabController;

    public UIOverlayController Overlay       => _overlay;
    public UIHUD HUD                         => _hud;
    public UIDialogueController Dialogue     => _dialogue;
    public UINotificationController Notification => _notification;
    public UIMenuTabController MenuTabController => _menuTabController;

    public void Register(UIHUD hud)                         => _hud = hud;
    public void Register(UIDialogueController dialogue)     => _dialogue = dialogue;
    public void Register(UINotificationController notification) => _notification = notification;
    public void Register(UIMenuTabController menu)          => _menuTabController = menu;

    public void Unregister(UIHUD _)                         => _hud = null;
    public void Unregister(UIDialogueController _)          => _dialogue = null;
    public void Unregister(UINotificationController _)      => _notification = null;
    public void Unregister(UIMenuTabController _)           => _menuTabController = null;
}
