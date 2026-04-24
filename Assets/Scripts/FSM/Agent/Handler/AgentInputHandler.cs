using UniRx;
[System.Serializable]
public class AgentInputHandler 
{
    protected IAgentInputListener _controller;
    public virtual void Initialize(IAgentInputListener controller)
    {
        _controller = controller;
        BindEvents();
    }
    protected virtual void BindEvents()
    {
        _controller.CombatInput.AttackPressed
            .Where(attackType => attackType != 0)
            .Subscribe(attackType => _controller.OnAttackAction(attackType))
            .AddTo(_controller.gameObject);
    }
}
