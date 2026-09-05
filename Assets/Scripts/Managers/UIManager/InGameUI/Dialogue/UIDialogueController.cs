using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class UIDialogueController : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _speakerText;
    [SerializeField] private TextMeshProUGUI _contentText;

    public bool IsOpen { get; private set; }

    [SerializeField] private InputAction _advanceAction;

    private Action _onAdvance;

    private void Awake()
    {
        _advanceAction.performed += _ => { if (IsOpen) _onAdvance?.Invoke(); };
    }

    public void Open(string speaker, string content, Action onAdvance)
    {
        _onAdvance = onAdvance;
        _speakerText.text = speaker;
        _contentText.text = content;
        _panel.SetActive(true);
        IsOpen = true;
        Managers.Instance.Player.Input?.SetInputBlocked(true);
        Managers.Instance.UI.InGameUI?.SetHUDActive(false);
    }

    public void UpdateContent(string content)
    {
        _contentText.text = content;
    }

    public void Close()
    {
        _onAdvance = null;
        _panel.SetActive(false);
        IsOpen = false;
        Managers.Instance.Player.Input?.SetInputBlocked(false);
        Managers.Instance.UI.InGameUI?.SetHUDActive(true);
    }

    private void OnEnable() => _advanceAction.Enable();
    private void OnDisable() => _advanceAction.Disable();
}
