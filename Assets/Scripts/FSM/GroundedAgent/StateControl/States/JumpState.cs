namespace ProjectRE
{
public class JumpState : AgentStateBase
{
    private readonly IGroundedAnimation _groundedAnimation;
    private readonly IAirborneAnimation _airborneAnimation;
    private AgentMovementHandler2D _movementHandler;
    private IAgentMovementInput _moveInput;
    
    public JumpState(IGroundedAnimation groundedAnimation, IAirborneAnimation airborneAnimation, AgentMovementHandler2D movementHandler, IAgentMovementInput moveInput)
    {
        _groundedAnimation = groundedAnimation;
        _airborneAnimation = airborneAnimation;
        _movementHandler = movementHandler;
        _moveInput = moveInput;
    }

    protected override void OnEnter() 
    {
        _groundedAnimation.SetGrounded(false);
        _airborneAnimation.SetJump(true);
        _movementHandler.HandleJump();
    }

    protected override void OnExecute(float deltaTime)
    {
        _movementHandler.HandleMove(_moveInput.GetMovementInput());
    }

    public override void Exit() 
    {
        _airborneAnimation.SetJump(false);
    }
}
}
