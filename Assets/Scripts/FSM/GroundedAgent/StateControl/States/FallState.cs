public class FallState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentMovementHandler2D _movementHandler;
    private IAgentMovementInput _moveInput;

    public FallState(AgentAnimator animator, AgentMovementHandler2D movementHandler, IAgentMovementInput moveInput)
    {
        _animator = animator;
        _movementHandler = movementHandler;
        _moveInput = moveInput;
    }
    protected override void OnEnter()
    {
        _animator.SetBool(StateType.Fall, true);
    }

    protected override void OnExecute(float deltaTime)
    {
        _movementHandler.HandleAirMove(_moveInput.GetMovementInput());
    }
    public override void Exit()
    {
        _animator.SetBool(StateType.Fall, false);
    }

}
