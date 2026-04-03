using UniRx;
[System.Serializable]
public class PlayerHealthHandler
{
    private Health _health;
    private AgentAnimationHandler _animationHandler;
    private PlayerController _controller;

    public void Initialize(Health health,AgentAnimationHandler animationHandler, PlayerController controller)
    {
        _health = health;
        _animationHandler = animationHandler;
        _controller = controller;
        BindEvents();
    }

    private void BindEvents()
    {
        _health.CurrentHealth
            .Pairwise()
            .Where(pair => pair.Current < pair.Previous)
            .Subscribe(_ => Hit())
            .AddTo(_controller);

        _health.IsDead
            .Pairwise() 
            .Where(pair => pair.Current != pair.Previous)
            .Subscribe(_ => Die())
            .AddTo(_controller);
    }
    private void Hit()
    {
        _animationHandler.ApplyHitAnimation(true);
        _controller.ChangeState(PlayerStateType.Hit);
    }

    private void Die()
    {
        _animationHandler.ApplyDieAnimation();
        _controller.ChangeState(PlayerStateType.Die);
    }
}
