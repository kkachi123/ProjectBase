using UnityEngine;

public class InGameUI : MonoBehaviour
{
    [SerializeField] private UIHUD _hud;
    [SerializeField] private UIDialogueController _dialogue;
    [SerializeField] private UINotificationController _notification;
    [SerializeField] private UIMenuTabController _menuTabController;

    public UIHUD HUD                         => _hud;
    public UIDialogueController Dialogue     => _dialogue;
    public UINotificationController Notification => _notification;
    public UIMenuTabController MenuTabController => _menuTabController;

    void Awake() => Managers.Instance.UI.Register(this);
    void OnDestroy() { if (Managers.Instance) Managers.Instance.UI.Unregister(this); }

    public void SetHUDActive(bool active) => _hud?.gameObject.SetActive(active);
}
