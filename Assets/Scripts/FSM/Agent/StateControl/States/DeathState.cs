namespace ProjectRE
{
using UnityEngine;
public class DeathState : AgentStateBase
{
    private IDeathAnimation _animator;
    private AgentCombatHandler _combatHandler;
    private AgentMovementHandler2D _movementHandler;

    public DeathState(IDeathAnimation animator, AgentCombatHandler combatHandler, AgentMovementHandler2D movementHandler)
    {
        _animator = animator;
        _combatHandler = combatHandler;
        _movementHandler = movementHandler;
    }

    protected override void OnEnter()
    {
        _animator.SetDeath(true);
        _combatHandler.ResetAttackType();
        _movementHandler.HandleMove(Vector2.zero);
    }

    protected override void OnExecute(float deltaTime)
    {
        return;
    }
    public override void Exit() { }
}
}
