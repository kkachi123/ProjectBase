using UniRx;
[System.Serializable]
public class PlayerInputHandler 
{
    private IAgentMovementInput _input;
    private IAgentCombatInput _combatInput;
    private AgentCombatHandler _combatHandler;
    private PlayerController _controller;
    public PlayerInputHandler SetController(IAgentMovementInput input , PlayerController playerController)
    {
        _input = input;
        _controller = playerController;
        BindJumpEvent();
        return this;
    }
    public void SetCombat(IAgentCombatInput combatInput, AgentCombatHandler combatHandler)
    {
        _combatInput = combatInput;
        _combatHandler = combatHandler;
        BindAttackEvent();
    }
    

    private void BindJumpEvent()
    {
        _input.JumpPressed
            .Where(jumpPressed => jumpPressed) // 점프 버튼이 눌렸을 때만 반응
            .Where(_ => _controller.IsGrounded && _controller.CanJump) // 현재 상태에서 점프가 가능한지 확인
            .Subscribe(_ => _controller.ChangeState(PlayerStateType.Jump))
            .AddTo(_controller);
    }

    private void BindAttackEvent()
    {
        _combatInput.AttackPressed
            .Where(attackType => attackType != 0)
            .Where(_ => _controller.IsGrounded)
            .Subscribe(attackType =>
            {
                _combatHandler.SetAttackType(attackType);
                _controller.ChangeState(PlayerStateType.Attack);
            })
            .AddTo(_controller);
    }
}
