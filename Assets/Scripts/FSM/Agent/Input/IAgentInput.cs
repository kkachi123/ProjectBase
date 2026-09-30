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
    // 점프 유지 여부를 확인하는 속성
    bool IsJumpHeld { get; }
    // 점프 입력이 발생했을 때 호출되는 이벤트
    event Action OnJumpRequested;
}

public interface IAgentInteractionInput
{
    event Action OnInteractRequested;
}
}
