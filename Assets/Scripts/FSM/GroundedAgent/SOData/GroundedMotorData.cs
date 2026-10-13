namespace ProjectRE
{
using UnityEngine;

[CreateAssetMenu(fileName = "GroundedMotorData", menuName = "Agent/Grounded Motor Data")]
public class GroundedMotorData : ScriptableObject
{
    public float knockbackForce = 3f;
    [Header("Grounded Settings")]
    public float moveSpeed = 5f;
    public float airMoveSpeed = 3f;
    public float jumpForce = 8f;
}
}
