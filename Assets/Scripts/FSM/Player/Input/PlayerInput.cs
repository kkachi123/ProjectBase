using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;

public class PlayerInput : MonoBehaviour , IAgentMovementInput , IAgentCombatInput
{
    public PlayerInputCommands inputActions;
    
    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<bool> _jumpPressed = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> _attackPressed = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<bool> JumpPressed => _jumpPressed;
    public IReadOnlyReactiveProperty<int> AttackPressed => _attackPressed;

    private void Awake()
    {
        inputActions = new PlayerInputCommands();

        inputActions.gamePlay.Move.performed += MoveInput;
        inputActions.gamePlay.Move.canceled += MoveInput;

        inputActions.gamePlay.Jump.performed += JumpInput;
        inputActions.gamePlay.Jump.canceled += JumpInput;

        inputActions.gamePlay.Attack1.performed += Attack1Input;
        inputActions.gamePlay.Attack1.canceled += AttackEnd;

        inputActions.gamePlay.Attack2.performed += Attack2Input;
        inputActions.gamePlay.Attack2.canceled += AttackEnd;
    }

    public Vector2 GetMovementInput()
    {
        return Horizontal;
    }

    public void MoveInput(InputAction.CallbackContext context)
    {
        if(context.canceled)
        {
            Horizontal = Vector2.zero;
            return;
        }
        Vector2 input = context.ReadValue<Vector2>();
        Horizontal = input.normalized;
    }

    public void JumpInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _jumpPressed.Value = true;
        }
        else if (context.canceled)
        {
            _jumpPressed.Value = false;
        }
    }
    public void AttackEnd(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            _attackPressed.Value = 0;
        }
    }
    public void Attack1Input(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _attackPressed.Value = 1;
        }
    }

    public void Attack2Input(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _attackPressed.Value = 2;
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}
