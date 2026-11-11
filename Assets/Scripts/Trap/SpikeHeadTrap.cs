namespace ProjectRE
{
    using UnityEngine;

    /// <summary>아래의 Player를 감지하면 예고 후 낙하하고 시작점으로 복귀한다.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    [RequireComponent(typeof(TrapContactDamage))]
    public sealed class SpikeHeadTrap : MonoBehaviour
    {
        private enum Phase { Waiting, Warning, Dropping, Returning, Cooldown }

        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0.1f)] private float detectionDistance = 8f;
        [SerializeField, Min(0.1f)] private float detectionWidth = 2.5f;
        [SerializeField, Min(0.1f)] private float warningDuration = 1f;
        [SerializeField, Min(0.1f)] private float maxDropDistance = 8f;
        [SerializeField, Min(0.01f)] private float dropSpeed = 12f;
        [SerializeField, Min(0.01f)] private float returnSpeed = 2f;
        [SerializeField, Min(0f)] private float cooldownDuration = 1f;
        [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f, 1f);

        private readonly Collider2D[] detected = new Collider2D[8];
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[1];
        private Rigidbody2D body;
        private BoxCollider2D damageCollider;
        private SpriteRenderer spriteRenderer;
        private Vector3 originLocal;
        private Vector2 start;
        private Color originalColor;
        private ContactFilter2D detectionFilter;
        private ContactFilter2D groundFilter;
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
            start = parent != null ? parent.TransformPoint(originLocal) : originLocal;
            body.position = start;
            detectionFilter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = false };
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers, useTriggers = false };
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
                        phase = Phase.Dropping;
                    }
                    break;
                case Phase.Dropping:
                    if (Drop()) phase = Phase.Returning;
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
            int groundCount = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down,
                groundFilter, groundHits, detectionDistance);
            float depth = groundCount > 0 ? Mathf.Min(detectionDistance, groundHits[0].distance) : detectionDistance;
            if (depth <= 0f) return false;
            Vector2 center = new Vector2(bounds.center.x, bounds.min.y - depth * 0.5f);
            int count = Physics2D.OverlapBox(center, new Vector2(detectionWidth, depth), 0f, detectionFilter, detected);
            for (int i = 0; i < count; i++)
                if (detected[i].GetComponentInParent<PlayerController>() != null) return true;
            return false;
        }

        private bool Drop()
        {
            const float skin = 0.01f;
            float remainingDistance = Mathf.Max(0f, maxDropDistance - (start.y - body.position.y));
            float distance = Mathf.Min(dropSpeed * Time.fixedDeltaTime, remainingDistance);
            if (distance <= 0f) return true;

            Bounds bounds = damageCollider.bounds;
            int count = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down,
                groundFilter, groundHits, distance + skin);
            float move = count > 0 ? Mathf.Max(0f, groundHits[0].distance - skin) : distance;
            body.MovePosition(body.position + Vector2.down * move);
            return count > 0 || distance >= remainingDistance;
        }

        private bool MoveTo(Vector2 target, float speed)
        {
            Vector2 next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
            body.MovePosition(next);
            return next == target;
        }
    }
}
