using UnityEngine;

public class FallState : PlayerStateBase
{
    public FallState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        Debug.Log("FallState Entered");
        _player.Falling(true);
    }

    public override void Execute()
    {
        _player.Move();
        if (_player.IsGrounded)
        {
            _player.ChangeState(PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        _player.Falling(false);
    }

}
