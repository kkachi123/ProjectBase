using UnityEngine;
public class P_DeathState : PlayerStateBase
{
    public P_DeathState(PlayerController player) : base(player) { }
    public override void Enter()
    {
        Debug.Log("P_DeathState Entered");
    }
    public override void Execute()
    {
        // Player is dead, no movement or actions allowed
    }
    public override void Exit() { }

}
