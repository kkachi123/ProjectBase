using UniRx;
public class PlayerInputHandler 
{
    private IAgentMovementInput _input;
    private PlayerController _playerController;
    public PlayerInputHandler(IAgentMovementInput input, PlayerController playerController)
    {
        _input = input;
        _playerController = playerController;
        SubscribeToInput();
    }

    private void SubscribeToInput()
    {
        _input.JumpPressed
            .Where(jumpPressed => jumpPressed && _playerController.IsGrounded) // 점프 버튼이 눌렸을 때만 반응
            .Where(_ => _playerController.CanJump) // 현재 상태에서 점프가 가능한지 확인
            .Subscribe(_ => _playerController.ChangeState(PlayerStateType.Jump))
            .AddTo(_playerController);
    }
}
