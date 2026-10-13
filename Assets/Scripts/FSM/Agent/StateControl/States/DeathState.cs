namespace ProjectRE
{
public class DeathState : AgentStateBase
{
    private IDeathAnimation _animator;
    private AgentCombatHandler _combatHandler;
    private IAgentMovementStop _movementHandler;

    public DeathState(IDeathAnimation animator, AgentCombatHandler combatHandler, IAgentMovementStop movementHandler)
    {
        _animator = animator;
        _combatHandler = combatHandler;
        _movementHandler = movementHandler;
    }

    protected override void OnEnter()
    {
        _animator.SetDeath(true);
        _combatHandler.ResetAttackType();
        _movementHandler.Stop();
    }

    protected override void OnExecute(float deltaTime)
    {
        return;
    }
    protected override void OnExit() { }
}
}
