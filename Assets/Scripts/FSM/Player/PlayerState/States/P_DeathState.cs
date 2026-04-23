using UnityEngine;
public class P_DeathState : PlayerStateBase
{
    public P_DeathState(PlayerController player) : base(player) { }
    public override void Enter()
    {
        _player.Death(true);
    }
    public override void Execute()
    {
        // Player is dead, no movement or actions allowed
    }
    public override void Exit() { }

}
