using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuTabController : MonoBehaviour
{
    [SerializeField] Sprite TabActive;
    [SerializeField] Sprite TabInactive;

    [SerializeField] List<UIScreen> _screens;
    [SerializeField] List<Button>   _tabs;

    int _activeIndex = -1;

    void Start()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            int idx = i;
            _tabs[i].onClick.AddListener(() => SwitchTo(idx));
        }
        SwitchTo(0);
    }

    public void SwitchTo(int index)
    {
        if (index == _activeIndex) return;

        if (_activeIndex >= 0 && _activeIndex < _screens.Count)
            _screens[_activeIndex].OnExit();

        _activeIndex = index;
        _screens[_activeIndex].OnEnter();

        for (int i = 0; i < _tabs.Count; i++)
        {
            var img = _tabs[i].GetComponent<Image>();
            if (img) img.sprite = i == _activeIndex ? TabActive : TabInactive;
        }
    }
}
