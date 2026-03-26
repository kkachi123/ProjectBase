using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour , IAgentMovementInput
{
    public PlayerInputCommands inputActions;
    
    public Vector2 Horizontal { get; private set; }
    public bool JumpPressed { get; private set; }

    private void Awake()
    {
        inputActions = new PlayerInputCommands();

        inputActions.gamePlay.Move.performed += MoveInput;
        inputActions.gamePlay.Move.canceled += MoveInput;

        inputActions.gamePlay.Jump.performed += JumpInput;
        inputActions.gamePlay.Jump.canceled += JumpInput;
    }

    public Vector2 GetMovementInput()
    {
        return Horizontal;
    }
    public bool IsJumpPressed()
    {
        return JumpPressed;
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
            JumpPressed = true;
        }
        else if (context.canceled)
        {
            JumpPressed = false;
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
