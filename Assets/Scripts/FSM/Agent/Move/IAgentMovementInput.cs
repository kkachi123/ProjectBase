using UniRx;
using UnityEngine;
public interface IAgentMovementInput
{
    Vector2 GetMovementInput();
} 

public interface IAgentJumpInput
{
    IReadOnlyReactiveProperty<bool> JumpPressed { get; }
}