using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCDialogue : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueDataSO _data;

    private readonly DialogueSequencePlayer _sequencePlayer = new();

    private void OnEnable()
    {
        _sequencePlayer.RegisterDialogue(Managers.Instance?.UI?.InGameUI?.Dialogue);
    }
    
    private void OnDisable()
    {
        _sequencePlayer.UnregisterDialogue();
    }

    public void Interact()
    {
        _sequencePlayer.Play(_data);
    }
}
