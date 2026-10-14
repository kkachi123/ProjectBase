namespace ProjectRE
{
public class DeathState : AgentStateBase
{
    private IDeathAnimation _animator;
    private IAgentMovementStop _movementHandler;

    public DeathState(IDeathAnimation animator, IAgentMovementStop movementHandler)
    {
        _animator = animator;
        _movementHandler = movementHandler;
    }

    protected override void OnEnter()
    {
        _animator.SetDeath(true);
        _movementHandler.Stop();
    }

    protected override void OnExecute(float deltaTime)
    {
        return;
    }
    protected override void OnExit() { }
}
}
