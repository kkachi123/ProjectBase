using System;
public interface IAnimationEventSource
{
    event Action OnAnimationEnded;
}
public class GetHitEndTransition : IEventTransitionRule
{
    public StateType NextState => StateType.Idle;
    private readonly IAnimationEventSource _eventSource;
    private bool _isSubscribed;
    private bool _shouldTransition;

    public GetHitEndTransition(IAnimationEventSource eventSource)
    {
        _eventSource = eventSource;
    }

    public void Subscribe()
    {
        if (_isSubscribed)
            return;

        _eventSource.OnAnimationEnded += TriggerTransition;
        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (_isSubscribed)
        {
            _eventSource.OnAnimationEnded -= TriggerTransition;
            _isSubscribed = false;
        }

        _shouldTransition = false;
    }

    public bool ShouldTransition(float deltaTime)
    {
        if (!_shouldTransition)
            return false;

        _shouldTransition = false;
        return true;
    }

    private void TriggerTransition()
    {
        _shouldTransition = true;
    }
    
}
