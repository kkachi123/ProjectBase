namespace ProjectRE
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Pool;

    /// <summary>발사 시점과 방향을 관리하는 투명 함정 발사기.</summary>
    public sealed class ArrowTrapLauncher : MonoBehaviour
    {
        [SerializeField] private ArrowTrapProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool fireLeft;
        [SerializeField, Min(0f)] private float initialDelay = 2f;
        [SerializeField, Min(0.01f)] private float fireInterval = 2f;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 5f;
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 4f;

        private readonly HashSet<ArrowTrapProjectile> projectiles = new();
        private ObjectPool<ArrowTrapProjectile> projectilePool;

        private void Awake()
        {
            projectilePool = new ObjectPool<ArrowTrapProjectile>(
                () => Instantiate(projectilePrefab, transform),
                projectile => projectile.gameObject.SetActive(true),
                projectile => projectile.gameObject.SetActive(false),
                projectile => { if (projectile != null) Destroy(projectile.gameObject); },
                true, 4, 32);
        }

        private void OnEnable()
        {
            if (projectilePrefab == null || firePoint == null)
            {
                Debug.LogError("화살 함정에 투사체 또는 발사 위치가 지정되지 않았습니다.", this);
                return;
            }

            StartCoroutine(FireRepeatedly());
        }

        private IEnumerator FireRepeatedly()
        {
            yield return new WaitForSeconds(initialDelay);
            while (true)
            {
                Vector3 localDirection = fireLeft ? Vector3.left : Vector3.right;
                Vector2 direction = transform.TransformVector(localDirection).normalized;
                ArrowTrapProjectile projectile = projectilePool.Get();
                projectile.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
                projectiles.Add(projectile);
                projectile.Launch(this, direction, projectileSpeed, damage, projectileLifetime);
                yield return new WaitForSeconds(fireInterval);
            }
        }

        internal void Return(ArrowTrapProjectile projectile)
        {
            if (!projectiles.Remove(projectile)) return;
            projectile.ResetForPool();
            projectilePool.Release(projectile);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            foreach (ArrowTrapProjectile projectile in projectiles)
            {
                if (projectile == null) continue;
                projectile.ResetForPool();
                projectilePool.Release(projectile);
            }
            projectiles.Clear();
        }

        private void OnDestroy() => projectilePool?.Clear();
    }
}
