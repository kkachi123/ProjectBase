using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionHandler : MonoBehaviour
{
    [SerializeField] private float _interactRadius = 2f;
    [SerializeField] private LayerMask _interactLayer;

    private PlayerInput _playerInput;
    private InputAction _interactAction;

    private readonly Collider2D[] _hitBuffer = new Collider2D[8];
    private IInteractable _nearbyTarget;
    private IInteractable _activeNPC;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _interactAction = new InputAction("Interact", InputActionType.Button);
        _interactAction.AddBinding("<Keyboard>/e");
        _interactAction.performed += OnInteract;
    }

    private void Update()
    {
        if (Managers.Instance.UI.Dialogue?.IsOpen == true) return;
        DetectNearbyTarget();
    }

    private void DetectNearbyTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, _interactRadius, _hitBuffer, _interactLayer);
        _nearbyTarget = null;

        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].TryGetComponent(out IInteractable interactable))
            {
                _nearbyTarget = interactable;
                break;
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        var dialogue = Managers.Instance.UI.Dialogue;
        if (dialogue == null) return;

        if (dialogue.IsOpen)
        {
            _activeNPC?.Interact();
            if (!dialogue.IsOpen)
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

    private void OnDestroy() => _interactAction.performed -= OnInteract;
}
