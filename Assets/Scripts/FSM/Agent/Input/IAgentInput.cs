using UniRx;
using UnityEngine;
public interface IAgentCombatInput
{
    IReadOnlyReactiveProperty<int> AttackPressed { get; }
}

public interface IAgentMovementInput
{
    Vector2 GetMovementInput();
} 

public interface IAgentInteractionInput
{
    IReadOnlyReactiveProperty<bool> InteractPressed { get; }
}