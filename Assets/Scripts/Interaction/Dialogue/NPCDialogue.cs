using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCDialogue : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueDataSO _data;

    private UIDialogueController _dialogue;
    private int _lineIndex;

    void Start()
    {
        InGameUI inGameUI = Managers.Instance.UI.InGameUI;
        if (inGameUI) _dialogue = inGameUI.Dialogue;
    }

    void OnDestroy() => _dialogue = null;

    public void Interact()
    {
        if (_dialogue == null || _data == null || _data.Lines.Length == 0) return;

        if (!_dialogue.IsOpen)
        {
            _lineIndex = 0;
            _dialogue.Open(_data.SpeakerName, _data.Lines[0]);
            return;
        }

        _lineIndex++;
        if (_lineIndex >= _data.Lines.Length)
            _dialogue.Close();
        else
            _dialogue.UpdateContent(_data.Lines[_lineIndex]);
    }
}
