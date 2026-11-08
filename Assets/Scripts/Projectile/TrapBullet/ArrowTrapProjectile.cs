namespace ProjectRE
{
    using UnityEngine;

    /// <summary>화살의 이동, 충돌 피해와 수명을 관리한다.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class ArrowTrapProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask terrainLayers;

        private ArrowTrapLauncher owner;
        private Rigidbody2D body;
        private float damage;
        private float remainingLifetime;
        private bool launched;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        public void Launch(ArrowTrapLauncher launcher, Vector2 direction, float speed, float damageAmount, float lifetime)
        {
            owner = launcher;
            damage = damageAmount;
            remainingLifetime = lifetime;
            launched = true;
            body.linearVelocity = direction.normalized * speed;
        }

        private void Update()
        {
            if (!launched) return;
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f) ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched) return;

            int layer = 1 << other.gameObject.layer;
            if ((targetLayers.value & layer) != 0)
            {
                IDamageable target = other.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(damage);
                    ReturnToPool();
                }
            }
            else if (!other.isTrigger && (terrainLayers.value & layer) != 0)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (!launched) return;
            owner.Return(this);
        }

        internal void ResetForPool()
        {
            launched = false;
            body.linearVelocity = Vector2.zero;
            owner = null;
        }
    }
}
