using System;
using UnityEngine;

public class NotifyDialogue : MonoBehaviour
{
    [SerializeField] private DialogueDataSO _clearMessage;
    [SerializeField] private DialogueDataSO _gameOverMessage;

    private readonly DialogueSequencePlayer _sequencePlayer = new();

    public void RegisterDialogue(UIDialogueController dialogue)
    {
        _sequencePlayer.RegisterDialogue(dialogue);
    }

    public void UnregisterDialogue()
    {
        _sequencePlayer.UnregisterDialogue();
    }

    public bool PlayClear(Action onFinished = null)
    {
        return _sequencePlayer.Play(_clearMessage, onFinished);
    }

    public bool PlayGameOver(Action onFinished = null)
    {
        return _sequencePlayer.Play(_gameOverMessage, onFinished);
    }
}
