using System;
using UnityEngine;
public interface IAgentCombatInput
{
    int HeldAttackType { get; }
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
