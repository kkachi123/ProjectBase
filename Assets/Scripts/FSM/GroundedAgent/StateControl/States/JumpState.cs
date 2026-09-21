public class JumpState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentMovementHandler2D _movementHandler;
    private IAgentMovementInput _moveInput;
    
    public JumpState(AgentAnimator animator, AgentMovementHandler2D movementHandler, IAgentMovementInput moveInput)
    {
        _animator = animator;
        _movementHandler = movementHandler;
        _moveInput = moveInput;
    }

    protected override void OnEnter() 
    {
        _animator.SetBool(StateType.Jump, true);
        _movementHandler.HandleJump();
    }

    protected override void OnExecute(float deltaTime)
    {
        _movementHandler.HandleMove(_moveInput.GetMovementInput());
    }

    public override void Exit() 
    {
        _animator.SetBool(StateType.Jump, false);
    }
}
