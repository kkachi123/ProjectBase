using System.Collections.Generic;
public class StateFactoryData
{
    public AgentAnimator Animator { get; set; }
    public AgentMovementHandler2D MovementHandler { get; set; }
    public IAgentMovementInput MovementInput { get; set; }
    public AgentCombatHandler CombatHandler { get; set; }
    public IAgentCombatInput CombatInput { get; set; }
}

public abstract class AgentStateFactory
{
    protected StateFactoryData stateFactoryData;

    public abstract Dictionary<StateType, AgentStateBase> CreateStates(AgentController agentController);
}
