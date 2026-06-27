using UnityEngine;
using UnityEngine.UI;

public class UIHUD : MonoBehaviour
{
    [SerializeField] private Slider _hpBar;
    [SerializeField] private Slider _staminaBar;
    [SerializeField] private GameObject _miniMap;

    public void SetHP(float current, float max)
    {
        _hpBar.value = current / max;
    }

    public void SetStamina(float current, float max)
    {
        _staminaBar.value = current / max;
    }

    public void SetMiniMapActive(bool active)
    {
        _miniMap.SetActive(active);
    }
}
