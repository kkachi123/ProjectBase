public abstract class PlayerStateBase : IAgentState
{
    protected PlayerController _player;
    public PlayerStateBase(PlayerController playerController)
    {
        _player = playerController;
    }
    public abstract void Enter();
    public abstract void Execute();
    public virtual void FixedExecute() { }
    public abstract void Exit();

    public virtual void OnAnimationEvent(AnimEventType type) { }
    public virtual void OnInputEvent(InputKeyType type) { }
}