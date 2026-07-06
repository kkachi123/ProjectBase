using UnityEngine;

public class UIHUD : MonoBehaviour
{
    [SerializeField] private GameObject _miniMap;

    public void SetMiniMapActive(bool active)
    {
        _miniMap.SetActive(active);
    }

    public void OnClickMenuButton()
    {
        Managers.Instance.UI.InGameUI.MenuTabController.ToggleMenu();
    }
}
