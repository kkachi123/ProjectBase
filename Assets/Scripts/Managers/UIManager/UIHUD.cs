using UnityEngine;

public class UIHUD : MonoBehaviour
{
    void Awake() => Managers.Instance.UI.Register(this);
    void OnDestroy() { if (Managers.Instance != null) Managers.Instance.UI.Unregister(this); }

    [SerializeField] private GameObject _miniMap;

    public void SetMiniMapActive(bool active)
    {
        _miniMap.SetActive(active);
    }

    public void OnClickMenuButton()
    {
        Managers.Instance.UI.MenuTabController.ToggleMenu();
    }
}
