public abstract class GroundedAgentStateBase<T> : AgentStateBase<T> where T : GroundedAgentController
{
    public GroundedAgentStateBase(T agentController) : base(agentController) { }
}

public abstract class GroundedAgentStateBase : GroundedAgentStateBase<GroundedAgentController>
{
    public GroundedAgentStateBase(GroundedAgentController agentController) : base(agentController) { }
}