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

    void Start() => Initialize();

    public void OpenMenu()
    {
        _content.SetActive(true);
        IsOpen = true;
        Managers.Instance.UI.SetHUDActive(false);
    }

    public void CloseMenu()
    {
        _content.SetActive(false);
        IsOpen = false;
        Managers.Instance.UI.SetHUDActive(true);
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
