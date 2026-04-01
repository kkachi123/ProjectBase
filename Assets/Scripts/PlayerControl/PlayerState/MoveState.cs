using UnityEngine;
public class MoveState : PlayerStateBase
{
    public override bool CanJump => true;
    public MoveState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        Debug.Log("MoveState Entered");
    }

    public override void Execute()
    {
        _player.Move();
        if (!_player.IsGrounded) _player.ChangeState(PlayerStateType.Fall);
        else if (_player.IsIdle) _player.ChangeState(PlayerStateType.Idle);
    }

    public override void Exit() { }

}
