public interface ITransitionRule
{
    StateType NextState { get; }
    bool ShouldTransition(float deltatime);
}

public interface IEventTransitionRule : ITransitionRule
{
    void Subscribe();
    void Unsubscribe();
}