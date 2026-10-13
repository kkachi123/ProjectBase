namespace ProjectRE
{
using System;

public class GroundedFallTransition : ITransitionRule
{
    private GroundDetector _groundDetector;
    private GroundedMotor2D _motor;
    public Type NextStateType => typeof(FallState);

    public GroundedFallTransition(GroundDetector groundDetector, GroundedMotor2D motor)
    {
        _groundDetector = groundDetector;
        _motor = motor;
    }

    public bool ShouldTransition(float deltatime)
    {
        return !_groundDetector.IsGrounded && _motor.VerticalVelocity < 0f;
    }
}
}
