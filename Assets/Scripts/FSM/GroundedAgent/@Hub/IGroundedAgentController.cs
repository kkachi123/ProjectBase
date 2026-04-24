using UnityEngine;
public interface IGroundedAgentInputListener
{
    GameObject gameObject { get; }
    IAgentJumpInput JumpInput { get; }
    void OnJumpAction();
}