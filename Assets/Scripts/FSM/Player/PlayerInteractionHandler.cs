using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionHandler : MonoBehaviour
{
    [SerializeField] private float _interactRadius = 2f;
    [SerializeField] private LayerMask _interactLayer;

    private PlayerInput _playerInput;
    private InputAction _interactAction;

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

        _interactAction = new InputAction("Interact", InputActionType.Button);
        _interactAction.AddBinding("<Keyboard>/e");
        _interactAction.performed += OnInteract;
    }

    private void Start()
    {
        InGameUI inGameUI = Managers.Instance.UI.InGameUI;
        if (inGameUI) _dialogue = inGameUI.Dialogue;
    }

    private void OnDestroy()
    {
        _interactAction.performed -= OnInteract;
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

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (_dialogue == null) return;

        if (_dialogue.IsOpen)
        {
            _activeNPC?.Interact();
            if (!_dialogue.IsOpen)
            {
                _playerInput.SetInputBlocked(false);
                _activeNPC = null;
            }
            return;
        }

        if (_nearbyTarget != null)
        {
            _activeNPC = _nearbyTarget;
            _playerInput.SetInputBlocked(true);
            _nearbyTarget.Interact();
        }
    }

    private void OnEnable() => _interactAction.Enable();
    private void OnDisable() => _interactAction.Disable();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}
