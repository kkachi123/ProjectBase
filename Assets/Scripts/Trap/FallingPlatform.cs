namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Player가 밟으면 예고 후 낙하하고 시작 위치가 비었을 때 복구한다.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class FallingPlatform : MonoBehaviour
    {
        private enum Phase { Ready, Warning, Falling, WaitingToRestore }

        [SerializeField, Min(0f)] private float warningDuration = 0.8f;
        [SerializeField, Min(0.01f)] private float fallSpeed = 5f;
        [SerializeField, Min(0.1f)] private float fallDistance = 4f;
        [SerializeField, Min(0f)] private float restoreDelay = 3f;
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private Color warningColor = new Color(1f, 0.55f, 0.35f, 1f);

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private readonly Collider2D[] occupants = new Collider2D[8];
        private Rigidbody2D body;
        private BoxCollider2D platform;
        private SpriteRenderer visual;
        private Vector3 originLocal;
        private Vector2 origin;
        private Color originalColor;
        private ContactFilter2D groundFilter;
        private ContactFilter2D playerFilter;
        private Phase phase;
        private float remaining;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            platform = GetComponent<BoxCollider2D>();
            visual = GetComponent<SpriteRenderer>();
            originLocal = transform.localPosition;
            originalColor = visual.color;
        }

        private void OnEnable()
        {
            origin = transform.parent != null ? transform.parent.TransformPoint(originLocal) : originLocal;
            body.position = origin;
            body.linearVelocity = Vector2.zero;
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers, useTriggers = false };
            playerFilter = new ContactFilter2D { useLayerMask = true, layerMask = playerLayers, useTriggers = false };
            platform.enabled = true;
            visual.enabled = true;
            visual.color = originalColor;
            phase = Phase.Ready;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (phase != Phase.Ready || collision.collider.GetComponentInParent<PlayerController>() == null) return;
            if (collision.collider.bounds.min.y < platform.bounds.max.y - 0.1f) return;
            phase = Phase.Warning;
            remaining = warningDuration;
            visual.color = warningColor;
        }

        private void FixedUpdate()
        {
            switch (phase)
            {
                case Phase.Warning:
                    remaining -= Time.fixedDeltaTime;
                    if (remaining <= 0f) phase = Phase.Falling;
                    break;
                case Phase.Falling:
                    Fall();
                    break;
                case Phase.WaitingToRestore:
                    remaining -= Time.fixedDeltaTime;
                    if (remaining <= 0f && OriginIsFree()) Restore();
                    break;
            }
        }

        private void Fall()
        {
            const float skin = 0.02f;
            float distance = Mathf.Min(fallSpeed * Time.fixedDeltaTime, fallDistance - (origin.y - body.position.y));
            if (distance <= 0f) { Hide(); return; }
            int count = platform.Cast(Vector2.down, groundFilter, groundHits, distance + skin);
            float move = count > 0 ? Mathf.Max(0f, groundHits[0].distance - skin) : distance;
            body.MovePosition(body.position + Vector2.down * move);
            if (count > 0 || origin.y - body.position.y + move >= fallDistance) Hide();
        }

        private void Hide()
        {
            platform.enabled = false;
            visual.enabled = false;
            remaining = restoreDelay;
            phase = Phase.WaitingToRestore;
        }

        private bool OriginIsFree()
        {
            Vector2 center = origin + Vector2.Scale(platform.offset, transform.lossyScale);
            Vector2 size = Vector2.Scale(platform.size, transform.lossyScale);
            int count = Physics2D.OverlapBox(center, size, 0f, playerFilter, occupants);
            for (int i = 0; i < count; i++)
                if (occupants[i].GetComponentInParent<PlayerController>() != null) return false;
            return true;
        }

        private void Restore()
        {
            body.position = origin;
            visual.color = originalColor;
            platform.enabled = true;
            visual.enabled = true;
            phase = Phase.Ready;
        }
    }
}
