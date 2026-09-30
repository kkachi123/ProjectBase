namespace ProjectRE
{
using System;
using UnityEngine;
public interface IAgentCombatInput
{
    event Action OnAttackRequested;
}

public interface IAgentMovementInput
{
    Vector2 GetMovementInput();
}

public interface IAgentJumpInput
{
    bool IsJumpHeld { get; }
    bool TryConsumeJumpRequest();
}

public interface IAgentInteractionInput
{
    event Action OnInteractRequested;
}
}
