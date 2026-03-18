using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public PlayerInputCommands inputActions;
    
    // 위아래 입력 시 Vector2 사용
    public float Horizontal { get; private set; }
    public bool JumpPressed { get; private set; }

    private void Awake()
    {
        inputActions = new PlayerInputCommands();

        inputActions.gamePlay.Move.performed += MoveInput;
        inputActions.gamePlay.Move.canceled += MoveInput;

        inputActions.gamePlay.Jump.performed += JumpInput;
    }

    public void MoveInput(InputAction.CallbackContext context)
    {
        if(context.canceled)
        {
            Horizontal = 0f; 
            return;
        }
        Vector2 input = context.ReadValue<Vector2>();
        Horizontal = input.x;
    }

    public void JumpInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            JumpPressed = true;
        }
    }

    public void AfterUseJump()
    {
        JumpPressed = false; 
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
