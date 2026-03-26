using UnityEngine;
public class IdleState : PlayerStateBase
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("IdleState Entered");
        _player.PlayerAnimator.ApplyMovementAnimation(Vector2.zero);
    }

    public override void Execute()
    {
        if(!_player.IsGrounded)
        {
            _player.ChangeState(PlayerStateType.Fall);
        }
        _player.Jump();
        _player.Move();
    }

    public override void Exit() { }

}
