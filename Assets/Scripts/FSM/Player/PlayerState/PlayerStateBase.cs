public enum AnimEventType
{
    OnFrame,
    End,
}
public enum InputKeyType
{
    None,
    Jump,
    Attack,
}
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

    public virtual void OnAnimationEvent(AnimEventType type) { }
    public virtual void OnInputEvent(InputKeyType type) { }
}