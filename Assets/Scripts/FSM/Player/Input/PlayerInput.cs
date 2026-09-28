namespace ProjectRE
{
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour , IAgentMovementInput , IAgentJumpInput , IAgentCombatInput , IAgentInteractionInput
{
    public PlayerInputCommands inputActions;

    public Vector2 Horizontal { get; private set; }
    private bool _jumpRequested;
    private int _attackRequestCount;
    public bool IsJumpHeld { get; private set; }
    public bool HasAttackRequest => _attackRequestCount > 0;
    public event System.Action OnAttackRequestsCleared;
    public event System.Action OnInteractRequested;

    public bool IsInputBlocked { get; private set; }

    public void SetInputBlocked(bool blocked)
    {
        IsInputBlocked = blocked;
        if (blocked)
        {
            Horizontal = Vector2.zero;
            IsJumpHeld = false;
            _jumpRequested = false;
            ClearAttackRequests();
        }
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
            _attackRequestCount++;
    }

    // _attackRequestCount를 1씩 감소
    // 0보다 작다 = 공격 완료, 0보다 크다 = 공격 입력이 남아있음
    public bool TryConsumeAttackRequest()
    {
        if (_attackRequestCount <= 0)
            return false;

        _attackRequestCount--;
        return true;
    }

    public void ClearAttackRequests()
    {
        _attackRequestCount = 0;
        OnAttackRequestsCleared?.Invoke();
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
