namespace ProjectRE
{
using System;

public class JumpTransition : IEventTransitionRule
{
    public Type NextStateType => typeof(JumpState);

    private readonly IAgentJumpInput _jumpInput;
    private readonly GroundDetector _groundDetector;
    private bool _isSubscribed;
    private bool _shouldTransition;

    public JumpTransition(IAgentJumpInput jumpInput, GroundDetector groundDetector)
    {
        _jumpInput = jumpInput;
        _groundDetector = groundDetector;
    }
    public bool ShouldTransition(float deltatime)
    {
        if (!_shouldTransition)
            return false;

        _shouldTransition = false;
        return _groundDetector.IsGrounded;
    }

    public void Subscribe()
    {
        if (_isSubscribed)
            return;

        _jumpInput.OnJumpRequested += TriggerTransition;
        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (_isSubscribed)
        {
            _jumpInput.OnJumpRequested -= TriggerTransition;
            _isSubscribed = false;
        }

        _shouldTransition = false;
    }

    private void TriggerTransition()
    {
        _shouldTransition = true;
    }
}
}
