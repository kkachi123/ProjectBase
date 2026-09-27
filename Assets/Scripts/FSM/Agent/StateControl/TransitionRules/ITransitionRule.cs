namespace ProjectRE
{
using System;

public interface ITransitionRule
{
    Type NextStateType { get; }
    bool ShouldTransition(float deltatime);
}

public interface IEventTransitionRule : ITransitionRule
{
    void Subscribe();
    void Unsubscribe();
}
}
