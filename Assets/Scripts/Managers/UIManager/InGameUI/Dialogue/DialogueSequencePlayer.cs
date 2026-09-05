using System;

public class DialogueSequencePlayer
{
    private UIDialogueController _dialogue;
    private DialogueDataSO _currentData;
    private Action _onFinished;
    private int _lineIndex;

    public bool IsPlaying { get; private set; }

    public void RegisterDialogue(UIDialogueController dialogue)
    {
        _dialogue = dialogue;
    }

    public void UnregisterDialogue()
    {
        Cancel();
        _dialogue = null;
    }

    public bool Play(DialogueDataSO data, Action onFinished = null)
    {
        if (_dialogue == null || data == null || data.Lines == null || data.Lines.Length == 0)
            return false;

        if (_dialogue.IsOpen || IsPlaying)
            return false;

        _currentData = data;
        _onFinished = onFinished;
        _lineIndex = 0;
        IsPlaying = true;

        _dialogue.Open(_currentData.SpeakerName, _currentData.Lines[_lineIndex], Advance);
        return true;
    }

    public void Cancel()
    {
        if (!IsPlaying) return;

        CloseCurrentDialogue();
        ClearPlaybackState();
    }

    private void Advance()
    {
        if (!IsPlaying || _currentData == null) return;

        _lineIndex++;
        if (_lineIndex >= _currentData.Lines.Length)
        {
            Finish();
            return;
        }

        if (_dialogue == null)
        {
            Finish();
            return;
        }

        _dialogue.UpdateContent(_currentData.Lines[_lineIndex]);
    }

    private void Finish()
    {
        Action onFinished = _onFinished;

        CloseCurrentDialogue();
        ClearPlaybackState();
        onFinished?.Invoke();
    }

    private void CloseCurrentDialogue()
    {
        if (_dialogue != null && _dialogue.IsOpen)
            _dialogue.Close();
    }

    private void ClearPlaybackState()
    {
        _currentData = null;
        _onFinished = null;
        _lineIndex = 0;
        IsPlaying = false;
    }
}
