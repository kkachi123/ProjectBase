public abstract class GroundedAgentStateBase : IAgentState
{
    protected GroundedAgentController _agent;
    public GroundedAgentStateBase(GroundedAgentController agentController)
    {
        _agent = agentController;
    }
    public abstract void Enter();
    public abstract void Execute();
    public virtual void FixedExecute() { }
    public abstract void Exit();

    public virtual void OnAnimationEvent(AnimEventType type) { }
    public virtual void OnInputEvent(InputKeyType type) { }
}