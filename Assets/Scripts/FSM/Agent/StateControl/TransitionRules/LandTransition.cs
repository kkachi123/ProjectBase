public class LandTransition : ITransitionRule
{
    private IAgentMovementInput _moveInput;
    public StateType NextState => _moveInput.GetMovementInput().x != 0 ? StateType.Move : StateType.Idle;
    private GroundDetector _groundDetector;

    public LandTransition(IAgentMovementInput moveInput, GroundDetector groundDetector)
    {
        _moveInput = moveInput;
        _groundDetector = groundDetector;
    }

    public bool ShouldTransition(float deltatime)
    {
        return _groundDetector.IsGrounded;
    }
}
