using UniRx;
using UnityEngine;
public interface IAgentMovementInput
{
    Vector2 GetMovementInput();
    IReadOnlyReactiveProperty<bool> JumpPressed { get; }
} 
