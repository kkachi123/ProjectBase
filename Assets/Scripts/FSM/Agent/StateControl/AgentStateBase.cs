public abstract class AgentStateBase<T> : IAgentState where T : AgentController
{
    protected T _agent;
    public AgentStateBase(T agentController)
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

public abstract class AgentStateBase : AgentStateBase<AgentController>
{
    public AgentStateBase(AgentController agentController) : base(agentController) { }
}