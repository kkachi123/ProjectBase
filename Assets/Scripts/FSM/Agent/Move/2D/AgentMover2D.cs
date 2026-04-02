using UnityEngine;

public class AgentMover2D : MonoBehaviour
{
    [SerializeField] AgentMotor2D _motor;

    public Vector2 Velocity => _motor.Velocity;

    public void Move(Vector2 horizontalInput , float moveSpeed)
    {
        _motor.Move(horizontalInput , moveSpeed);
    }

    public void Jump(Vector2 horizontalInput ,float jumpForce)
    {
        _motor.Jump(horizontalInput , jumpForce);
    }
}
