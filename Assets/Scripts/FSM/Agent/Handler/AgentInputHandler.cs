using UniRx;
public class AgentInputHandler 
{
    protected IAgentMovementInput _input;
    protected IAgentCombatInput _combatInput;
    protected IAgentInputListener _controller;

    public virtual void Initialize(IAgentInputListener controller)
    {
        _controller = controller;
        _input = controller.MoveInput;
        _combatInput = controller.CombatInput;
        BindEvents();
    }

    protected virtual void BindEvents()
    {
        _combatInput.AttackPressed
            .Where(attackType => attackType != 0)
            .Subscribe(attackType => _controller.OnAttackAction(attackType))
            .AddTo(_controller.gameObject);
    }
}
