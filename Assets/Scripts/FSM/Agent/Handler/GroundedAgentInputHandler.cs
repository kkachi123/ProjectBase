using UniRx;
public class GroundedAgentInputHandler : AgentInputHandler
{
    private IAgentJumpInput _jumpInput;

    public void BindJumpInput(IAgentJumpInput jumpInput)
    {
        _jumpInput = jumpInput;
        if (_jumpInput != null)
        {
            _jumpInput.JumpPressed
                .Where(jumpPressed => jumpPressed)
                .Subscribe(_ => _controller.OnJumpAction())
                .AddTo(_controller.gameObject);
        }
    }
}
