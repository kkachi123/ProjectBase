using UnityEngine;
public class JumpState : PlayerStateBase
{
    private float _jumpTimer;
    private const float MIN_JUMP_TIME = 0.1f;
    public JumpState(PlayerController player) : base(player) { _player = player; }

    public override void Enter() 
    {
        _jumpTimer = 0f;
        _player.PlayerInput.AfterUseJump();

        float velocityX = _player.PlayerInput.Horizontal * _player.PlayerStateData.jumpSpeed;
        _player.PlayerMotor.Jump(velocityX, _player.PlayerStateData.jumpForce);

        _player.PlayerAnimator.ApplyJumpingAnimation(true , false);
        Debug.Log("JumpState Entered");
    }

    public override void Execute()
    {
        float velocityX = _player.PlayerInput.Horizontal * _player.PlayerStateData.jumpSpeed;
        _player.PlayerMotor.Move(velocityX);
        _player.PlayerAnimator.ApplyMovementAnimation(velocityX);

        _jumpTimer += Time.deltaTime;
        if(_jumpTimer >= MIN_JUMP_TIME)
        {
            if (IsGrounded())
            {
                _player.ChangeState(PlayerStateType.Idle);
            }
            else if (IsFalling())
            {
                _player.ChangeState(PlayerStateType.Fall);
            }
        }
    }

    public override void Exit() 
    {
        _player.PlayerAnimator.ApplyJumpingAnimation(IsGrounded(), false);
    }

    bool IsGrounded()
    {
        return _player.IsGrounded;
    }

    bool IsFalling()
    {
        return _player.PlayerMotor.Velocity.y < 0f;
    }
}
