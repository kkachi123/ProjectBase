using UnityEngine;
public interface IAgentMovementInput
{
    Vector2 GetMovementInput();
    bool IsJumpPressed();
} 
