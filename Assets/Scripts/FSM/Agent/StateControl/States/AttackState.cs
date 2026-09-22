public class AttackState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentCombatHandler _combatHandler;
    private IAgentCombatInput _combatInput;
    private IAttackStarter _attackStarter;

    public AttackState(AgentAnimator animator , AgentCombatHandler combatHandler , IAgentCombatInput combatInput, IAttackStarter attackStarter)
    {
        _animator = animator;
        _combatHandler = combatHandler;
        _combatInput = combatInput;
        _attackStarter = attackStarter;
    }

    protected override void OnEnter()
    {
        int requestedType = _combatInput.AttackPressed.Value;

        if (!_attackStarter.TryStartAttack(requestedType))
            return;
        _animator.SetBool(StateType.Attack, true);
        _animator.SetInteger(AnimationIntType.AttackType, _combatHandler.CurrentAttackType);
    }
    protected override void OnExecute(float deltatime)
    {
        return;
    }
    
    public override void Exit() 
    { 
        _combatHandler.ResetAttackType();
        _animator.SetBool(StateType.Attack, false);
        _animator.SetInteger(AnimationIntType.AttackType, 0);
    }
}
