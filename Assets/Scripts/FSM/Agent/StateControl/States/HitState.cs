using UnityEngine;

public class HitState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentCombatHandler _combatHandler;

    public HitState(AgentAnimator animator, AgentCombatHandler combatHandler)
    {
        _animator = animator;
        _combatHandler = combatHandler;
    }
    protected override void OnEnter()
    {
        _combatHandler.ResetAttackType();
        _animator.SetBool(StateType.Hit, true);
    }
    protected override void OnExecute(float deltaTime)
    {
        return;
    }
    public override void Exit() 
    {
        return;
    }
}