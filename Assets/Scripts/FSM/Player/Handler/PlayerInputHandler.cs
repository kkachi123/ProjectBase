using UniRx;
[System.Serializable]
public class PlayerInputHandler 
{
    private IAgentMovementInput _input;
    private PlayerController _controller;
    public void Initialize(IAgentMovementInput input, PlayerController playerController)
    {
        _input = input;
        _controller = playerController;
        BindJumpEvent();
    }

    private void BindJumpEvent()
    {
        _input.JumpPressed
            .Where(jumpPressed => jumpPressed) // 점프 버튼이 눌렸을 때만 반응
            .Where(_ => _controller.IsGrounded && _controller.CanJump) // 현재 상태에서 점프가 가능한지 확인
            .Subscribe(_ => _controller.ChangeState(PlayerStateType.Jump))
            .AddTo(_controller);
    }
}
