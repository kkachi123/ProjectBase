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
            if (input.x > 0f) transform.localScale = new Vector3(1f, 1f, 1f);
            else if (input.x < 0f) transform.localScale = new Vector3(-1f, 1f, 1f);
            _body.linearVelocity = input * speed;
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
