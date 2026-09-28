namespace ProjectRE
{
using System;
using UnityEngine;
public interface IAgentCombatInput
{
    bool HasAttackRequest { get; }
    bool TryConsumeAttackRequest();
    void ClearAttackRequests();
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
