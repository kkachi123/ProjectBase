using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIOverlayController _overlay;
    private InGameUI _inGameUI;

    public UIOverlayController Overlay => _overlay;
    public InGameUI InGameUI           => _inGameUI;

    public void Register(InGameUI inGameUI)  => _inGameUI = inGameUI;
    public void Unregister(InGameUI _)       => _inGameUI = null;
}
