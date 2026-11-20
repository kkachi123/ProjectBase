namespace ProjectRE
{
    using UnityEngine;

    /// <summary>자유 XY 비행·전체 정지·넉백 실행.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FlyingMotor2D : MonoBehaviour, IAgentMotor2D
    {
        private Rigidbody2D _body;

        private void Awake() => _body = GetComponent<Rigidbody2D>();

        // 입력 방향에 따른 XY 속도·좌우 시선 적용.
        public void Move2D(Vector2 input, float speed)
        {
            Face(input.x);
            _body.linearVelocity = input * speed;
        }

        // 기존 크기를 유지하며 좌우 방향 전환.
        public void Face(float direction)
        {
            Vector3 scale = transform.localScale;
            if (direction > 0f) scale.x = Mathf.Abs(scale.x);
            else if (direction < 0f) scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        public void Stop() => _body.linearVelocity = Vector2.zero;

        // 현재 속도 초기화 후 넉백 힘 적용.
        public void Knockback(Vector3 direction, float force)
        {
            Stop();
            _body.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }
}
