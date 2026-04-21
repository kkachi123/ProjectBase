public interface IState
{
    public void Enter();
    public void Execute();
    public void Exit();
}

public interface IAgentState : IState
{
    public void FixedExecute();
}

public enum StateType
{
    Idle,
    Move,
    Jump,
    Fall,
    Attack,
    Hit,
    Death,
}