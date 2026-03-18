public abstract class PlayerStateBase : IState
{
    protected PlayerController _player;
    public PlayerStateBase(PlayerController playerController)
    {
        _player = playerController;
    }
    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}