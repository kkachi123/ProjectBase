using UnityEngine;

public class M_DeathState : MonsterStateBase
{
    public M_DeathState(MonsterController monster) : base(monster) { }
    public override void Enter()
    {
        Debug.Log("M_DeathState Entered");
    }
    public override void Execute()
    {
        // Monster is dead, no movement or actions allowed
    }
    public override void Exit() { }

}