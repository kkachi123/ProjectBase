using UnityEngine;
public class DeathState : AgentStateBase
{
    public DeathState(AgentController agent) : base(agent) { }
    public override void Enter()
    {
        _agent.Death(true);
    }
    public override void Execute()
    {
        // Player is dead, no movement or actions allowed
    }
    public override void Exit() { }

}
