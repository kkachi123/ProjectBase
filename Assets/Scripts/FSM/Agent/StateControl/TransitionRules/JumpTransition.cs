namespace ProjectRE
{
using System;

public class JumpTransition : ITransitionRule
{
    public Type NextStateType => typeof(JumpState);

    private IAgentJumpInput _jumpInput;
    private GroundDetector _groundDetector;

    public JumpTransition(IAgentJumpInput jumpInput, GroundDetector groundDetector)
    {
        _jumpInput = jumpInput;
        _groundDetector = groundDetector;
    }
    public bool ShouldTransition(float deltatime)
    {
        bool requested = _jumpInput.TryConsumeJumpRequest();
        return requested && _groundDetector.IsGrounded;
    }
}
}
