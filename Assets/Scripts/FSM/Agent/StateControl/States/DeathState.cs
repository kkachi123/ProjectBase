using UnityEngine;
public class DeathState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentCombatHandler _combatHandler;
    private AgentMovementHandler2D _movementHandler;

    public DeathState(AgentAnimator animator, AgentCombatHandler combatHandler, AgentMovementHandler2D movementHandler)
    {
        _animator = animator;
        _combatHandler = combatHandler;
        _movementHandler = movementHandler;
    }

    protected override void OnEnter()
    {
        _animator.SetBool(StateType.Death, true);
        _combatHandler.ResetAttackType();
        _movementHandler.HandleMove(Vector2.zero);
    }

    protected override void OnExecute(float deltaTime)
    {
        return;
    }
    public override void Exit() { }
}
