namespace ProjectRE
{
using System;

public class JumpFallTransition : ITransitionRule
{
    public Type NextStateType => typeof(FallState);

    private readonly AgentMotor2D _motor;

    public JumpFallTransition(AgentMotor2D motor)
    {
        _motor = motor;
    }

    public bool ShouldTransition(float deltatime)
    {
        return _motor.VerticalVelocity <= 0f;
    }
}
}
