using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuTabController : MonoBehaviour
{
    [SerializeField] Sprite TabActive;
    [SerializeField] Sprite TabInactive;

    [SerializeField] private GameObject _content;
    [SerializeField] List<UITab> _screens;
    [SerializeField] List<Button> _tabs;

    int _activeIndex = -1;
    Image[] _tabImages;

    public bool IsOpen { get; private set; }

    private void EnsureInit()
    {
        if (_tabImages != null) return;
        Initialize();
    }

    public void OpenMenu()
    {
        EnsureInit();
        _content.SetActive(true);
        IsOpen = true;
        _screens[_activeIndex].OnShow();
        Managers.Instance.UI.InGameUI?.SetHUDActive(false);
    }

    public void CloseMenu()
    {
        _screens[_activeIndex].OnHide();
        _content.SetActive(false);
        IsOpen = false;
        Managers.Instance.UI.InGameUI?.SetHUDActive(true);
    }

    public void ToggleMenu()
    {
        if (IsOpen) CloseMenu();
        else OpenMenu();
    }

    public void Initialize()
    {
        _tabImages = new Image[_tabs.Count];
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabImages[i] = _tabs[i].GetComponent<Image>();
            int idx = i;
            _tabs[i].onClick.AddListener(() => SwitchTo(idx));
            _tabs[i].onClick.AddListener(() => Debug.Log($"Tab {idx} clicked"));
        }
        SwitchTo(0);
    }

    public void SwitchTo(int index)
    {
        if (index == _activeIndex) return;

        int prevIndex = _activeIndex;

        if (prevIndex >= 0 && prevIndex < _screens.Count)
        {
            _screens[prevIndex].OnHide();
            if (_tabImages[prevIndex]) _tabImages[prevIndex].sprite = TabInactive;
        }

        _activeIndex = index;
        _screens[_activeIndex].OnShow();
        if (_tabImages[_activeIndex]) _tabImages[_activeIndex].sprite = TabActive;
    }
}
