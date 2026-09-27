namespace ProjectRE
{
using System;

public class LandTransition : ITransitionRule
{
    public Type NextStateType => typeof(GroundedState);
    private GroundDetector _groundDetector;
    private AgentMotor2D _motor;

    public LandTransition(GroundDetector groundDetector, AgentMotor2D motor)
    {
        _groundDetector = groundDetector;
        _motor = motor;
    }

    public bool ShouldTransition(float deltatime)
    {
        return _groundDetector.IsGrounded && _motor.VerticalVelocity <= 0f;
    }
}
}
