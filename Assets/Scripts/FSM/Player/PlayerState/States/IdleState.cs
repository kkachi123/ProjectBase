using UnityEngine;
public class IdleState : PlayerStateBase
{
    public override bool CanJump => true;
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("IdleState Entered");
    }

    public override void Execute()
    {
        if (!_player.IsGrounded) _player.ChangeState(PlayerStateType.Fall);
        else if(!_player.IsIdle) _player.ChangeState(PlayerStateType.Move);
    }

    public override void Exit() { }

}
