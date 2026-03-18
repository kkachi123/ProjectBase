using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/States")]
public class PlayerStateData : ScriptableObject
{
    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpSpeed = 3f;
    public float jumpForce = 10f;
}
