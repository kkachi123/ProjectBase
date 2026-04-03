using UniRx;
[System.Serializable]
public class AgentInputHandler 
{
    private IAgentMovementInput _input;
    private IAgentCombatInput _combatInput;
    private IAgentInputListener _controller;
    public void Initialize(IAgentInputListener controller)
    {
        _controller = controller;
        _input = controller.MoveInput;
        _combatInput = controller.CombatInput;
        BindEvents();
    }

    private void BindEvents()
    {
        _input.JumpPressed
            .Where(jumpPressed => jumpPressed) 
            .Subscribe(_ => _controller.OnJumpAction())
            .AddTo(_controller.gameObject);

        _combatInput.AttackPressed
            .Where(attackType => attackType != 0)
            .Subscribe(attackType => _controller.OnAttackAction(attackType))
            .AddTo(_controller.gameObject);
    }
}
