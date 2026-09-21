public class MoveState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentMovementHandler2D _movementHandler;
    private IAgentMovementInput _moveInput;

    public MoveState(AgentAnimator animator, AgentMovementHandler2D movementHandler, IAgentMovementInput moveInput)
    {
        _animator = animator;
        _movementHandler = movementHandler;
        _moveInput = moveInput;
    }

    protected override void OnEnter()
    {
        _animator.SetBool(StateType.Move, true);
    }

    protected override void OnExecute(float deltaTime)
    {
        //if (_agent.IsIdle) _agent.ChangeState(StateType.Idle);
        _movementHandler.HandleMove(_moveInput.GetMovementInput());
    }

    public override void Exit() 
    {
        _animator.SetBool(StateType.Move, false);
    }
}