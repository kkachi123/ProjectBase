public class AttackTransition : ITransitionRule
{
    public StateType NextState => StateType.Attack;

    private IAgentCombatInput _combatInput;
    private IAttackStarter _attackStarter;

    public AttackTransition(IAgentCombatInput combatInput , IAttackStarter attackStarter)
    {
        _combatInput = combatInput;
        _attackStarter = attackStarter;
    }
    public bool ShouldTransition(float deltatime)
    {
        int requestedType = _combatInput.AttackPressed.Value;

        return requestedType > 0
            && _attackStarter.CanStartAttack(requestedType);
    }
}
