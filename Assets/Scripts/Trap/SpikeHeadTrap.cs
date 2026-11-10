namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Player를 감지하면 예고 후 직선 돌진하고 시작점으로 복귀한다.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    [RequireComponent(typeof(TrapContactDamage))]
    public sealed class SpikeHeadTrap : MonoBehaviour
    {
        private enum Direction { Left, Right }
        private enum Phase { Waiting, Warning, Charging, Returning, Cooldown }

        [SerializeField] private Direction direction = Direction.Left;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField, Min(0.1f)] private float detectionDistance = 4f;
        [SerializeField, Min(0.1f)] private float detectionHeight = 2.5f;
        [SerializeField, Min(0.1f)] private float warningDuration = 1f;
        [SerializeField, Min(0f)] private float travelDistance = 3f;
        [SerializeField, Min(0.01f)] private float chargeSpeed = 5f;
        [SerializeField, Min(0.01f)] private float returnSpeed = 2f;
        [SerializeField, Min(0f)] private float cooldownDuration = 1f;
        [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f, 1f);

        private readonly Collider2D[] detected = new Collider2D[8];
        private Rigidbody2D body;
        private BoxCollider2D damageCollider;
        private SpriteRenderer spriteRenderer;
        private Vector3 originLocal;
        private Vector2 start;
        private Vector2 end;
        private Vector2 forward;
        private Color originalColor;
        private ContactFilter2D detectionFilter;
        private Phase phase;
        private float remaining;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            damageCollider = GetComponent<BoxCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            originLocal = transform.localPosition;
            originalColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            Transform parent = transform.parent;
            Vector3 localForward = direction == Direction.Left ? Vector3.left : Vector3.right;
            forward = parent != null ? (Vector2)parent.TransformDirection(localForward).normalized : (Vector2)localForward;
            start = parent != null ? parent.TransformPoint(originLocal) : originLocal;
            end = start + forward * travelDistance;
            body.position = start;
            detectionFilter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = false };
            remaining = 0f;
            phase = Phase.Waiting;
            spriteRenderer.color = originalColor;
        }

        private void OnDisable()
        {
            spriteRenderer.color = originalColor;
        }

        private void FixedUpdate()
        {
            switch (phase)
            {
                case Phase.Waiting:
                    if (DetectPlayer())
                    {
                        phase = Phase.Warning;
                        remaining = warningDuration;
                        spriteRenderer.color = warningColor;
                    }
                    break;
                case Phase.Warning:
                    remaining -= Time.fixedDeltaTime;
                    if (remaining <= 0f)
                    {
                        spriteRenderer.color = originalColor;
                        phase = Phase.Charging;
                    }
                    break;
                case Phase.Charging:
                    if (MoveTo(end, chargeSpeed)) phase = Phase.Returning;
                    break;
                case Phase.Returning:
                    if (MoveTo(start, returnSpeed))
                    {
                        phase = Phase.Cooldown;
                        remaining = cooldownDuration;
                    }
                    break;
                case Phase.Cooldown:
                    remaining -= Time.fixedDeltaTime;
                    if (remaining <= 0f) phase = Phase.Waiting;
                    break;
            }
        }

        private bool DetectPlayer()
        {
            Bounds bounds = damageCollider.bounds;
            float halfWidth = bounds.extents.x;
            Vector2 center = (Vector2)bounds.center + forward * (halfWidth + detectionDistance * 0.5f);
            int count = Physics2D.OverlapBox(center, new Vector2(detectionDistance, detectionHeight), 0f, detectionFilter, detected);
            for (int i = 0; i < count; i++)
                if (detected[i].GetComponentInParent<PlayerController>() != null) return true;
            return false;
        }

        private bool MoveTo(Vector2 target, float speed)
        {
            Vector2 next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
            body.MovePosition(next);
            return next == target;
        }
    }
}
