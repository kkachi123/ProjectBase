using UniRx;
public class GroundedAgentInputHandler
{
    private IGroundedAgentInputListener _controller;

    public GroundedAgentInputHandler(IGroundedAgentInputListener controller)
    {
        _controller = controller;
        BindEvents();
    }

    private void BindEvents()
    {
        _controller.JumpInput.JumpPressed
               .Where(jumpPressed => jumpPressed)
               .Subscribe(_ => _controller.OnJumpAction())
               .AddTo(_controller.gameObject);
    }
}
