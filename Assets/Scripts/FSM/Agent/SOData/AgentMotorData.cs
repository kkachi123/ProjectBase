using UnityEngine;

[CreateAssetMenu(fileName = "AgentStates", menuName = "Agent/States")]
public class AgentMotorData : ScriptableObject
{
    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpSpeed = 3f;
    public float jumpForce = 8f;
}
