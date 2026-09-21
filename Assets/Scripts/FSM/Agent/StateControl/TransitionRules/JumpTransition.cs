public class JumpTransition : ITransitionRule
{
    public StateType NextState => StateType.Jump;

    private IAgentJumpInput _jumpInput;

    public JumpTransition(IAgentJumpInput jumpInput)
    {
        _jumpInput = jumpInput;

    }
    public bool ShouldTransition(float deltatime)
    {
        return _jumpInput.JumpPressed.Value;
    }
}