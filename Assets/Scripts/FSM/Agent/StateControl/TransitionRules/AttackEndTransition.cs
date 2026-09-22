public class AttackEndTransition : IEventTransitionRule
{
    public StateType NextState => StateType.Idle;

    private readonly IAnimationEventSource _eventSource;
    private readonly IAgentCombatInput _combatInput;
    private readonly GroundDetector _groundDetector;

    private bool _isSubscribed;
    private bool _shouldTransition;

    public AttackEndTransition(IAnimationEventSource eventSource, IAgentCombatInput combatInput, GroundDetector groundDetector)
    {
        _eventSource = eventSource;
        _combatInput = combatInput;
        _groundDetector = groundDetector;
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
