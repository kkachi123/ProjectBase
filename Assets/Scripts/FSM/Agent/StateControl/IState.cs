public interface IState
{
    public void Enter();
    public void Execute();
    public void Exit();
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