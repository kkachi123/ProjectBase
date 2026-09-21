public class AttackTransition : ITransitionRule
{
    public StateType NextState => StateType.Attack;

    private IAgentCombatInput _combatInput;

    public AttackTransition(IAgentCombatInput combatInput)
    {
        _combatInput = combatInput;
    }
    public bool ShouldTransition(float deltatime)
    {
        return _combatInput.AttackPressed.Value > 0; 
    }
}
