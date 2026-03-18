using UnityEngine;
public class MoveState : PlayerStateBase
{
    public MoveState(PlayerController player) : base(player) { _player = player; }

    public override void Enter() { }

    public override void Execute()
    {
        float velocityX = _player.PlayerInput.Horizontal * _player.PlayerStateData.moveSpeed;
        _player.PlayerMotor.Move(velocityX);
        _player.PlayerAnimator.ApplyMovementAnimation(velocityX);

        if (IsIdle())
        {
            _player.ChangeState(PlayerStateType.Idle);
        }
        else if (IsJumpPressed())
        {
            _player.ChangeState(PlayerStateType.Jump);
        }
        else if (IsFalling())
        {
            _player.ChangeState(PlayerStateType.Fall);
        }
    }

    public override void Exit() { }

    bool IsIdle()
    {
        return Mathf.Abs(_player.PlayerInput.Horizontal) < 0.01f;
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
