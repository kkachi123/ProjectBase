namespace ProjectRE
{
    using UnityEngine;

    /// <summary>톱니를 초기 위치와 지정된 거리 사이에서 왕복시킨다.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SawPatrol : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float travelDistance = 3f;
        [SerializeField, Min(0.01f)] private float speed = 2f;

        private Rigidbody2D body;
        private Vector3 originLocal;
        private Vector2 start;
        private Vector2 end;
        private bool movingToEnd;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            originLocal = transform.localPosition;
        }

        private void OnEnable()
        {
            Transform parent = transform.parent;
            start = parent != null ? parent.TransformPoint(originLocal) : originLocal;
            Vector3 endLocal = originLocal + Vector3.right * travelDistance;
            end = parent != null ? parent.TransformPoint(endLocal) : endLocal;
            body.position = start;
            movingToEnd = true;
        }

        private void FixedUpdate()
        {
            Vector2 target = movingToEnd ? end : start;
            Vector2 next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
            body.MovePosition(next);
            if (next == target) movingToEnd = !movingToEnd;
        }
    }
}
