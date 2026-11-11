namespace ProjectRE
{
    using UnityEngine;

    /// <summary>가시 공을 시작 위치 위의 고정점을 중심으로 왕복시킨다.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SpikedBallSwing : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 2.5f;
        [SerializeField, Range(0f, 89f)] private float swingAngle = 70f;
        [SerializeField, Min(0.1f)] private float swingPeriod = 4f;
        [SerializeField] private Transform chain;

        private Rigidbody2D body;
        private SpriteRenderer chainRenderer;
        private Vector3 originLocal;
        private Vector2 center;
        private float elapsed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            chainRenderer = chain.GetComponent<SpriteRenderer>();
            originLocal = transform.localPosition;
        }

        private void OnEnable()
        {
            Transform parent = transform.parent;
            Vector3 start = parent != null ? parent.TransformPoint(originLocal) : originLocal;
            Vector3 pivotLocal = originLocal + Vector3.up * radius;
            center = parent != null ? parent.TransformPoint(pivotLocal) : pivotLocal;
            elapsed = 0f;
            body.position = start;
            chainRenderer.size = new Vector2(chainRenderer.size.x, radius);
            UpdateChain(start);
        }

        private void FixedUpdate()
        {
            elapsed = Mathf.Repeat(elapsed + Time.fixedDeltaTime, swingPeriod);
            float radians = swingAngle * Mathf.Sin(elapsed * 2f * Mathf.PI / swingPeriod) * Mathf.Deg2Rad;
            Vector2 next = center + new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians)) * radius;
            body.MovePosition(next);
        }

        private void LateUpdate() => UpdateChain(body.position);

        private void UpdateChain(Vector2 ballPosition)
        {
            Vector2 direction = center - ballPosition;
            chain.position = (Vector3)(ballPosition + direction * 0.5f);
            chain.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        }
    }
}
