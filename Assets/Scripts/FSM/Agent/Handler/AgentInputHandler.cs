using UniRx;
public class AgentInputHandler 
{
    private IAgentInputListener _controller;
    public AgentInputHandler(IAgentInputListener controller)
    {
        _controller = controller;
        BindEvents();
    }
    private void BindEvents()
    {
        _controller.CombatInput.AttackPressed
            .Where(attackType => attackType != 0)
            .Subscribe(attackType => _controller.OnAttackAction(attackType))
            .AddTo(_controller.gameObject);
    }
}
