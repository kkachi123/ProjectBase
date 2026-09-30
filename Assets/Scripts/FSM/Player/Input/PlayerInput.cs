namespace ProjectRE
{
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour , IAgentMovementInput , IAgentJumpInput , IAgentCombatInput , IAgentInteractionInput
{
    public PlayerInputCommands inputActions;

    public Vector2 Horizontal { get; private set; }
    private bool _jumpRequested;
    public bool IsJumpHeld { get; private set; }
    public event System.Action OnAttackRequested;
    public event System.Action OnInteractRequested;

    public bool IsInputBlocked { get; private set; }

    public void SetInputBlocked(bool blocked)
    {
        IsInputBlocked = blocked;
    }

    private void Awake()
    {
        // Test
        Managers.Instance?.Player.Register(this);
        inputActions = new PlayerInputCommands();

        inputActions.gamePlay.Move.performed += MoveInput;
        inputActions.gamePlay.Move.canceled += MoveInput;

        inputActions.gamePlay.Jump.performed += JumpInput;
        inputActions.gamePlay.Jump.canceled += JumpInput;

        inputActions.gamePlay.Attack.performed += AttackInput;

        inputActions.gamePlay.Interact.performed += InteractInput;
    }

    public Vector2 GetMovementInput()
    {
        return IsInputBlocked ? Vector2.zero : Horizontal;
    }

    public void MoveInput(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            Horizontal = Vector2.zero;
            return;
        }
        Vector2 input = context.ReadValue<Vector2>();
        Horizontal = input.normalized;
    }

    public void JumpInput(InputAction.CallbackContext context)
    {
        if (IsInputBlocked) return;
        if (context.performed)
        {
            IsJumpHeld = true;
            _jumpRequested = true;
        }
        else if (context.canceled)
            IsJumpHeld = false;
    }

    public bool TryConsumeJumpRequest()
    {
        bool requested = _jumpRequested;
        _jumpRequested = false;
        return requested;
    }

    private void AttackInput(InputAction.CallbackContext context)
    {
        if (!IsInputBlocked && context.performed)
            OnAttackRequested?.Invoke();
    }

    private void InteractInput(InputAction.CallbackContext context)
    {
        if (!IsInputBlocked && context.performed)
            OnInteractRequested?.Invoke();
    }

    private void OnDestroy()
    {
        if (Managers.Instance) Managers.Instance.Player.Unregister(this);
    }

    private void OnEnable() => inputActions.Enable();

    private void OnDisable() => inputActions.Disable();
}
}
