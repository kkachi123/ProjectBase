using UniRx;
[System.Serializable]
public class AgentHealthHandler
{
    private Health _health;
    private IAgentHealthListener _controller;

    public void Initialize(IAgentHealthListener controller)
    {
        _health = controller.Health;
        _controller = controller;
        BindEvents();
    }

    private void BindEvents()
    {
        _health.OnKnockback
            .Subscribe(knockbackDir => _controller.OnHit(knockbackDir))
            .AddTo(_controller.gameObject);

        _health.IsDead
            .Pairwise() 
            .Where(pair => pair.Current != pair.Previous)
            .Subscribe(_ => _controller.OnDeath())
            .AddTo(_controller.gameObject);
    }

}
