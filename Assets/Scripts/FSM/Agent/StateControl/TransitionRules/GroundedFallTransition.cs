using System;

public class GroundedFallTransition : ITransitionRule
{
    private GroundDetector _groundDetector;
    public Type NextStateType => typeof(FallState);

    public GroundedFallTransition(GroundDetector groundDetector)
    {
        _groundDetector = groundDetector;
    }

    public bool ShouldTransition(float deltatime)
    {
        return !_groundDetector.IsGrounded;
    }
}
