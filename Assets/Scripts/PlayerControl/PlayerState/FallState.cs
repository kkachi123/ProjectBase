using UnityEngine;

public class FallState : PlayerStateBase
{
    public FallState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        _player.PlayerAnimator.ApplyFallingAnimation(false);
        Debug.Log("FallState Entered");
    }

    public override void Execute()
    {
        float velocityX = _player.PlayerInput.Horizontal * _player.PlayerStateData.jumpSpeed;
        _player.PlayerMotor.Move(velocityX);
        _player.PlayerAnimator.ApplyMovementAnimation(velocityX);

        if (IsGrounded())
        {
            _player.PlayerAnimator.ApplyFallingAnimation(true);
            _player.ChangeState(PlayerStateType.Idle);
        }
    }

    public override void Exit() { }

    bool IsGrounded()
    {
        return _player.IsGrounded;
    }
}
