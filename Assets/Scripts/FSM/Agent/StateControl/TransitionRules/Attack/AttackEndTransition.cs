namespace ProjectRE
{
using System;

/// <summary>
/// 공격 Animation 종료 후 지상 여부에 따라 GroundedState·FallState로 복귀.
/// </summary>
public class AttackEndTransition : IEventTransitionRule
{
    public Type NextStateType => _groundDetector.IsGrounded ? typeof(GroundedState) : typeof(FallState);

    private readonly IAnimationEventSource _eventSource;
    private readonly GroundDetector _groundDetector;
    private bool _isSubscribed;
    private bool _shouldTransition;

    public AttackEndTransition(IAnimationEventSource eventSource, GroundDetector groundDetector)
    {
        _eventSource = eventSource;
        _groundDetector = groundDetector;
    }

    public bool ShouldTransition(float deltaTime)
    {
        if (!_shouldTransition)
            return false;

        _shouldTransition = false;
        return true;
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

    private void TriggerTransition()
    {
        _shouldTransition = true;
    }
}
}
