using UnityEngine;
public class IdleState : PlayerStateBase
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("IdleState Entered");
    }

    public override void Execute()
    {
        if(!_player.IsGrounded)
        {
            _player.ChangeState(PlayerStateType.Fall);
        }
        else 
        {
            if(!_player.IsIdle) _player.ChangeState(PlayerStateType.Move);
            else if (_player.IsJumping) _player.ChangeState(PlayerStateType.Jump);
        }
    }

    public override void Exit() { }

}
