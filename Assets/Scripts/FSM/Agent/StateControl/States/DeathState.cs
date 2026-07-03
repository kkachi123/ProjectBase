public class DeathState : AgentStateBase
{
    public DeathState(AgentController agent) : base(agent) { }

    public override void Enter()
    {
        _agent.Death(true);
    }

    public override void Execute() { }
    public override void Exit() { }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if (type == AnimEventType.End) _agent.OnDeathFinished();
    }
}
