namespace ProjectRE
{
public class HitState : AgentStateBase
{
    private IHitAnimation _animator;

    public HitState(IHitAnimation animator)
    {
        _animator = animator;
    }
    protected override void OnEnter()
    {
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
