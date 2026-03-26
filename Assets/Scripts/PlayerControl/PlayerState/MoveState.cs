using UnityEngine;
public class MoveState : PlayerStateBase
{
    public MoveState(PlayerController player) : base(player) { _player = player; }

    public override void Enter() { }

    public override void Execute()
    {
        if (!_player.IsGrounded)
        {
            _player.ChangeState(PlayerStateType.Fall);
        }
        _player.Jump();
        _player.Move();
    }

    public override void Exit() { }

}
