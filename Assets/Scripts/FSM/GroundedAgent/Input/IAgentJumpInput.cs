using UniRx;
public interface IAgentJumpInput
{
    IReadOnlyReactiveProperty<bool> JumpPressed { get; }
}