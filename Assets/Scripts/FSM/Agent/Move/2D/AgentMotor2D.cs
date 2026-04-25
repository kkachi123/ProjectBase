using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AgentMotor2D : MonoBehaviour
{
    Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 horizontalInput, float moveSpeed)
    {
        Turn(horizontalInput);
        _rb.linearVelocityX = horizontalInput.x * moveSpeed;
    }

    private void Turn(Vector2 horizontalInput)
    {
        if (horizontalInput.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    public void Jump(float jumpForce)
    {
        _rb.AddForceY(jumpForce, ForceMode2D.Impulse);
    }

    public void Knockback(Vector3 horizontalInput, float knockForce)
    {
        // X축 속도를 초기화하고 넉백 힘을 적용
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(horizontalInput * knockForce, ForceMode2D.Impulse);
    }
}
