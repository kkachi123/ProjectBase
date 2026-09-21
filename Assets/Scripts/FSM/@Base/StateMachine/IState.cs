public interface IState
{
    public void Enter();
    public void Execute();
    public void Exit();
}


public enum StateType
{
    // Common States
    Idle,
    Move,
    Attack,
    Hit,
    Death,
    // Grounded States
    Jump,
    Fall,
}