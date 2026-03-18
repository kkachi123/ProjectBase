using UnityEngine;
public class DieState : PlayerStateBase
{
    public DieState(PlayerController player) : base(player) { _player = player; }
    public override void Enter()
    {
        _player.PlayerAnimator.ApplyDieAnimation();
        Debug.Log("DieState Entered");
    }
    public override void Execute()
    {
        // Player is dead, no movement or actions allowed
    }
    public override void Exit() { }

}
