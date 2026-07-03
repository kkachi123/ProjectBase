using UnityEngine;
using TMPro;

public class UIDialogueController : MonoBehaviour
{
    void Awake() => Managers.Instance.UI.Register(this);
    void OnDestroy() { if (Managers.Instance != null) Managers.Instance.UI.Unregister(this); }

    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _speakerText;
    [SerializeField] private TextMeshProUGUI _contentText;

    public bool IsOpen { get; private set; }

    public void Open(string speaker, string content)
    {
        _speakerText.text = speaker;
        _contentText.text = content;
        _panel.SetActive(true);
        IsOpen = true;
        Managers.Instance.UI.SetHUDActive(false);
    }

    public void UpdateContent(string content)
    {
        _contentText.text = content;
    }

    public void Close()
    {
        _panel.SetActive(false);
        IsOpen = false;
        Managers.Instance.UI.SetHUDActive(true);
    }
}
