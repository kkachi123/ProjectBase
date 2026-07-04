using UnityEngine;
using UniRx;

public class PlayerInteractionHandler : MonoBehaviour
{
    [SerializeField] private float _interactRadius = 2f;
    [SerializeField] private LayerMask _interactLayer;

    private PlayerInput _playerInput;

    private readonly Collider2D[] _hitBuffer = new Collider2D[8];
    private ContactFilter2D _contactFilter;
    private IInteractable _nearbyTarget;
    private IInteractable _activeNPC;

    private UIDialogueController _dialogue;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_interactLayer);
        _contactFilter.useTriggers = true;

        _playerInput.InteractPressed
            .Where(v => v)
            .Subscribe(_ => OnInteract())
            .AddTo(this);
    }

    private void Start()
    {
        InGameUI inGameUI = Managers.Instance.UI.InGameUI;
        if (inGameUI) _dialogue = inGameUI.Dialogue;
    }

    private void OnDestroy()
    {
        _dialogue = null;
    }

    private void Update()
    {
        if (_dialogue && _dialogue.IsOpen) return;
        DetectNearbyTarget();
    }

    private void DetectNearbyTarget()
    {
        int count = Physics2D.OverlapCircle(transform.position, _interactRadius, _contactFilter, _hitBuffer);
        _nearbyTarget = null;

        float minDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].TryGetComponent(out IInteractable interactable)) continue;
            float dist = Vector2.SqrMagnitude(_hitBuffer[i].transform.position - transform.position);
            if (dist >= minDist) continue;
            minDist = dist;
            _nearbyTarget = interactable;
        }
    }

    private void OnInteract()
    {
        if (_dialogue == null) return;

        if (_dialogue.IsOpen) ContinueDialogue();
        else TryStartInteraction();
    }

    private void ContinueDialogue()
    {
        _activeNPC?.Interact();
        if (_dialogue.IsOpen) return;
        _playerInput.SetInputBlocked(false);
        _activeNPC = null;
    }

    private void TryStartInteraction()
    {
        if (_nearbyTarget == null) return;
        _activeNPC = _nearbyTarget;
        _playerInput.SetInputBlocked(true);
        _nearbyTarget.Interact();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}
