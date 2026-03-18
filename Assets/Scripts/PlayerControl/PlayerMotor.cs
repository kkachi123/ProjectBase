using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    Rigidbody2D _rb;
    public Vector2 Velocity => _rb.linearVelocity;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Move(float velocityX)
    {
        _rb.linearVelocity = new Vector2(velocityX, _rb.linearVelocity.y);
    }

    public void Jump(float velocityX, float jumpForce)
    {
        // 점프 전에 수직 속도를 초기화
        _rb.linearVelocity = new Vector2(velocityX, 0); 
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    public void StopMovement()
    {
        _rb.linearVelocity = Vector2.zero;
    }

}
