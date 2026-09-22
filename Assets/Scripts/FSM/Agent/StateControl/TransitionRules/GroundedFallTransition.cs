public class GroundedFallTransition : ITransitionRule
{
    private GroundDetector _groundDetector;
    public StateType NextState => StateType.Fall;

    public GroundedFallTransition(GroundDetector groundDetector)
    {
        _groundDetector = groundDetector;
    }

    public bool ShouldTransition(float deltatime)
    {
        return !_groundDetector.IsGrounded;
    }
}
