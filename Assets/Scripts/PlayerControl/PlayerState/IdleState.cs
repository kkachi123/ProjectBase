using UnityEngine;
public class IdleState : PlayerStateBase
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        _player.PlayerMotor.Move(0);

        _player.PlayerAnimator.ApplyFallingAnimation(true);
        _player.PlayerAnimator.ApplyMovementAnimation(0f);
        Debug.Log("IdleState Entered");
    }

    public override void Execute()
    {
        if(IsMovePressed())
        {
            _player.ChangeState(PlayerStateType.Move);
        }
        else if(IsJumpPressed())
        {
            _player.ChangeState(PlayerStateType.Jump);
        }
        else if(IsFalling())
        {
            _player.ChangeState(PlayerStateType.Fall);
        }
    }

    public override void Exit() { }
    bool IsMovePressed()
    {
        return Mathf.Abs(_player.PlayerInput.Horizontal) > 0.01f;
    }

    bool IsJumpPressed()
    {
        return _player.PlayerInput.JumpPressed;
    }

    bool IsFalling()
    {
        return !_player.IsGrounded;
    }
}
