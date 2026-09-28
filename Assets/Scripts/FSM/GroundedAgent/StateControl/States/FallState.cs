namespace ProjectRE
{
public class FallState : AgentStateBase
{
    private readonly IGroundedAnimation _groundedAnimation;
    private readonly IAirborneAnimation _airborneAnimation;
    private AgentMovementHandler2D _movementHandler;
    private IAgentMovementInput _moveInput;

    public FallState(IGroundedAnimation groundedAnimation, IAirborneAnimation airborneAnimation, AgentMovementHandler2D movementHandler, IAgentMovementInput moveInput)
    {
        _groundedAnimation = groundedAnimation;
        _airborneAnimation = airborneAnimation;
        _movementHandler = movementHandler;
        _moveInput = moveInput;
    }
    protected override void OnEnter()
    {
        _groundedAnimation.SetGrounded(false);
        _airborneAnimation.SetJump(false);
        _airborneAnimation.SetFall(true);
    }

    protected override void OnExecute(float deltaTime)
    {
        _movementHandler.HandleAirMove(_moveInput.GetMovementInput());
    }
    public override void Exit()
    {
        _airborneAnimation.SetFall(false);
    }

}
}
