namespace ProjectRE
{
using System;

public class LandTransition : ITransitionRule
{
    private IAgentMovementInput _moveInput;
    public Type NextStateType => _moveInput.GetMovementInput().x != 0 ? typeof(MoveState) : typeof(IdleState);
    private GroundDetector _groundDetector;
    private AgentMotor2D _motor;

    public LandTransition(IAgentMovementInput moveInput, GroundDetector groundDetector, AgentMotor2D motor)
    {
        _moveInput = moveInput;
        _groundDetector = groundDetector;
        _motor = motor;
    }

    public bool ShouldTransition(float deltatime)
    {
        return _groundDetector.IsGrounded && _motor.VerticalVelocity <= 0f;
    }
}
}
