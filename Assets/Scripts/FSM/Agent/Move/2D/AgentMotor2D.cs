using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AgentMotor2D : MonoBehaviour
{
    Rigidbody2D _rb;
    public Vector2 Velocity => _rb.linearVelocity;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 horizontalInput, float moveSpeed)
    {
        _rb.linearVelocity = new Vector2(horizontalInput.x * moveSpeed, _rb.linearVelocity.y);
    }

    public void Jump(Vector2 horizontalInput, float jumpForce)
    {
        // X축 속도를 초기화하고 점프 힘을 적용
        _rb.linearVelocity = new Vector2(horizontalInput.x, 0);
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    public void StopMovement()
    {
        _rb.linearVelocity = Vector2.zero;
    }

}
