using UnityEngine;
public class MoveState : PlayerStateBase
{
    public MoveState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        Debug.Log("MoveState Entered");
    }

    public override void Execute()
    {
        _player.Move();
        if (!_player.IsGrounded)
        {
            _player.ChangeState(PlayerStateType.Fall);
        }
        else
        {
            if (_player.IsIdle) _player.ChangeState(PlayerStateType.Idle);
            else if (_player.IsJumping) _player.ChangeState(PlayerStateType.Jump);
        }
    }

    public override void Exit() { }

}
