using UnityEngine;
using UnityEngine.UI;

public class UIHUD : MonoBehaviour
{
    void Awake() => Managers.Instance.UI.Register(this);
    void OnDestroy() { if (Managers.Instance != null) Managers.Instance.UI.Unregister(this); }

    [SerializeField] private Image _hpFill;
    [SerializeField] private Image _staminaFill;
    [SerializeField] private GameObject _miniMap;

    public void SetHP(float current, float max)
    {
        _hpFill.fillAmount = current / max;
    }

    public void SetStamina(float current, float max)
    {
        _staminaFill.fillAmount = current / max;
    }

    public void SetMiniMapActive(bool active)
    {
        _miniMap.SetActive(active);
    }

    public void OnClickMenuButton()
    {
        Managers.Instance.UI.MenuTabController.ToggleMenu();
    }
}
