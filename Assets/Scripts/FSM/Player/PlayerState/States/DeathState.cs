using UnityEngine;
public class DeathState : PlayerStateBase
{
    public DeathState(PlayerController player) : base(player) { _player = player; }
    public override void Enter()
    {
        Debug.Log("DeathState Entered");
    }
    public override void Execute()
    {
        // Player is dead, no movement or actions allowed
    }
    public override void Exit() { }

}
