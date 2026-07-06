using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;

public class PlayerInput : MonoBehaviour , IAgentMovementInput , IAgentJumpInput , IAgentCombatInput , IAgentInteractionInput
{
    public PlayerInputCommands inputActions;

    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<bool> _jumpPressed = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> _attackPressed = new ReactiveProperty<int>(0);
    private readonly ReactiveProperty<bool> _interactPressed = new(false);
    public IReadOnlyReactiveProperty<bool> JumpPressed => _jumpPressed;
    public IReadOnlyReactiveProperty<int> AttackPressed => _attackPressed;
    public IReadOnlyReactiveProperty<bool> InteractPressed => _interactPressed;

    public bool IsInputBlocked { get; private set; }

    public void SetInputBlocked(bool blocked)
    {
        IsInputBlocked = blocked;
        if (blocked)
        {
            Horizontal = Vector2.zero;
            _jumpPressed.Value = false;
            _attackPressed.Value = 0;
            _interactPressed.Value = false;
        }
    }

    private void Awake()
    {
        Managers.Instance.Player.Register(this);
        inputActions = new PlayerInputCommands();

        inputActions.gamePlay.Move.performed += MoveInput;
        inputActions.gamePlay.Move.canceled += MoveInput;

        inputActions.gamePlay.Jump.performed += JumpInput;
        inputActions.gamePlay.Jump.canceled += JumpInput;

        inputActions.gamePlay.Attack1.performed += context => AttackInput(context, 1);
        inputActions.gamePlay.Attack1.canceled += AttackEnd;

        inputActions.gamePlay.Attack2.performed += context => AttackInput(context, 2);
        inputActions.gamePlay.Attack2.canceled += AttackEnd;

        inputActions.gamePlay.Interact.performed += _ => { _interactPressed.Value = true; _interactPressed.Value = false; };
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
            _jumpPressed.Value = true;
        else if (context.canceled)
            _jumpPressed.Value = false;
    }

    public void AttackInput(InputAction.CallbackContext context, int value)
    {
        if (IsInputBlocked) return;
        if (context.performed)
            _attackPressed.Value = value;
    }

    public void AttackEnd(InputAction.CallbackContext context)
    {
        if (context.canceled)
            _attackPressed.Value = 0;
    }

    private void OnDestroy()
    {
        if (Managers.Instance) Managers.Instance.Player.Unregister(this);
    }

    private void OnEnable() => inputActions.Enable();

    private void OnDisable() => inputActions.Disable();
}
