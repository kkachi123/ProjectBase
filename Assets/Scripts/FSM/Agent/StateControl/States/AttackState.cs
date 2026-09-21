public class AttackState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentCombatHandler _combatHandler;

    public AttackState(AgentAnimator animator , AgentCombatHandler combatHandler)
    {
        _animator = animator;
        _combatHandler = combatHandler;
    }

    protected override void OnEnter()
    {
        _animator.SetBool(StateType.Attack, true);
        _animator.SetInteger(AnimationIntType.AttackType, _combatHandler.CurrentAttackType);
    }
    protected override void OnExecute(float deltatime)
    {
        return;
    }
    
    public override void Exit() 
    { 
        _animator.SetBool(StateType.Attack, false);
        _combatHandler.ResetAttackType();
        _animator.SetInteger(AnimationIntType.AttackType, 0);
    }
}
