namespace ProjectRE
{
using UnityEngine;

public class HitState : AgentStateBase
{
    private IHitAnimation _animator;
    private AgentCombatHandler _combatHandler;

    public HitState(IHitAnimation animator, AgentCombatHandler combatHandler)
    {
        _animator = animator;
        _combatHandler = combatHandler;
    }
    protected override void OnEnter()
    {
        _combatHandler.ResetAttackType();
        _animator.SetHit(true);
    }
    protected override void OnExecute(float deltaTime)
    {
        return;
    }
    public override void Exit() 
    {
        _animator.SetHit(false);
    }
}
}
