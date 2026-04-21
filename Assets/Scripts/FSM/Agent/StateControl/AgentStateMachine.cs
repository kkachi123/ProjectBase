public class AgentStateMachine<T> : StateMachine<T> where T : class, IAgentState
{
    public void FixedOperate()
    {
        _currentState?.FixedExecute();
    }
}
