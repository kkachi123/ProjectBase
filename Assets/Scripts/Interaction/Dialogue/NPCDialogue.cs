using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCDialogue : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueDataSO _data;

    private UIDialogueController _dialogue;
    private int _lineIndex;

    private void Awake()
    {
        Managers.Instance.UI.OnDialogueChanged += OnDialogueChanged;
        _dialogue = Managers.Instance.UI.Dialogue;
    }

    private void OnDestroy()
    {
        Managers.Instance.UI.OnDialogueChanged -= OnDialogueChanged;
        _dialogue = null;
    }

    private void OnDialogueChanged(UIDialogueController dialogue) => _dialogue = dialogue;

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
