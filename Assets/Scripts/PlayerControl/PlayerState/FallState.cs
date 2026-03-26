using UnityEngine;

public class FallState : PlayerStateBase
{
    public FallState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        _player.PlayerAnimator.ApplyFallingAnimation(true);
        Debug.Log("FallState Entered");
    }

    public override void Execute()
    {
        _player.Move();
    }

    public override void Exit()
    {
        _player.PlayerAnimator.ApplyFallingAnimation(false);
    }

}
