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
    protected override void OnExit()
    {
        _animator.SetHit(false);
    }
}
}
