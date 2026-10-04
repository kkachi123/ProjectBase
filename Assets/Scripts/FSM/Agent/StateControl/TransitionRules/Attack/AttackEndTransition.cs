namespace ProjectRE
{
using System;

/// <summary>
/// 공격 Animation 종료 후 지면 상태 또는 지정한 상태로 복귀.
/// </summary>
public class AttackEndTransition : IEventTransitionRule
{
    // _returnStateType가 null이면 지면 감지 여부에 따라 GroundedState 또는 FallState로 복귀.
    public Type NextStateType => _returnStateType ??
        (_groundDetector.IsGrounded ? typeof(GroundedState) : typeof(FallState));

    private readonly IAnimationEventSource _eventSource;
    private readonly GroundDetector _groundDetector;
    private readonly Type _returnStateType;
    private bool _isSubscribed;
    private bool _shouldTransition;

    public AttackEndTransition(IAnimationEventSource eventSource, GroundDetector groundDetector)
    {
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        _groundDetector = groundDetector ?? throw new ArgumentNullException(nameof(groundDetector));
    }

    /// <summary>지면 감지 없이 등록된 상태로 고정 복귀.</summary>
    public AttackEndTransition(IAnimationEventSource eventSource, Type returnStateType)
    {
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        _returnStateType = returnStateType ?? throw new ArgumentNullException(nameof(returnStateType));
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
