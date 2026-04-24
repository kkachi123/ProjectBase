public interface IState
{
    public void Enter();
    public void Execute();
    public void Exit();
}

public interface IAgentState : IState
{
    public void FixedExecute();
    public void OnAnimationEvent(AnimEventType type);
    public void OnInputEvent(InputKeyType type);
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

public enum AnimEventType
{
    OnFrame,
    End,
}
public enum InputKeyType
{
    None,
    Jump,
    Attack,
}