namespace ProjectRE
{
public class AttackState : AgentStateBase
{
    private ICombatAnimation _animator;
    private AgentCombatHandler _combatHandler;
    private IAgentCombatInput _combatInput;
    private IAttackStarter _attackStarter;

    public AttackState(ICombatAnimation animator , AgentCombatHandler combatHandler , IAgentCombatInput combatInput, IAttackStarter attackStarter)
    {
        _animator = animator;
        _combatHandler = combatHandler;
        _combatInput = combatInput;
        _attackStarter = attackStarter;
    }

    protected override void OnEnter()
    {
        int requestedType = _combatInput.HeldAttackType;

        if (!_attackStarter.TryStartAttack(requestedType))
            return;
        _animator.SetAttack(true);
        _animator.SetAttackType(_combatHandler.CurrentAttackType);
    }
    protected override void OnExecute(float deltatime)
    {
        return;
    }
    
    public override void Exit() 
    { 
        _combatHandler.ResetAttackType();
        _animator.SetAttack(false);
        _animator.SetAttackType(0);
    }
}
}
