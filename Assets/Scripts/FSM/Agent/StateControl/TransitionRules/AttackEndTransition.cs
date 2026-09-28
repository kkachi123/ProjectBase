namespace ProjectRE
{
using System;

public class AttackEndTransition : IEventTransitionRule
{
    public Type NextStateType => _groundDetector.IsGrounded ? typeof(GroundedState) : typeof(FallState);

    private readonly IAnimationEventSource _eventSource;
    private readonly GroundDetector _groundDetector;
    private readonly IAgentCombatInput _combatInput;
    private readonly IAttackComboStarter _attackStarter;
    private readonly ICombatAnimation _animator;
    private readonly ComboAttackHandler _comboHandler;

    private bool _isSubscribed;
    private bool _shouldProcessEnd;

    public AttackEndTransition(
        IAnimationEventSource eventSource,
        GroundDetector groundDetector,
        IAgentCombatInput combatInput,
        IAttackComboStarter attackStarter,
        ICombatAnimation animator,
        ComboAttackHandler comboHandler)
    {
        _eventSource = eventSource;
        _groundDetector = groundDetector;
        _combatInput = combatInput;
        _attackStarter = attackStarter;
        _animator = animator;
        _comboHandler = comboHandler;
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

        _shouldProcessEnd = false;
    }

    public bool ShouldTransition(float deltaTime)
    {
        if (!_shouldProcessEnd)
            return false;

        _shouldProcessEnd = false;
        return !_comboHandler.TryAdvanceGroundCombo(_combatInput, _attackStarter, _animator);
    }

    private void TriggerTransition()
    {
        _shouldProcessEnd = true;
    }
}
}
