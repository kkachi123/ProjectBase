namespace ProjectRE
{
    using UnityEngine;

    public class PlayerDetector : MonoBehaviour
    {
        [SerializeField] private LayerMask playerMask;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] Vector3 offset = new Vector3(0f, 1.0f, 0f);
        [Range(0, 20)]
        [SerializeField] private float viewRadius = 5f;

        public Transform Target { get; private set; }

        /// <summary>플레이어가 시야 내에 있는지 확인하고, 있으면 Target에 할당.</summary>
        public bool IsTargetInView()
        {
            // 1. 시야 원의 중심 위치 계산 (현재 위치 + 오프셋)
            Vector3 origin = transform.position + offset;
            Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(origin, viewRadius, playerMask);
            Collider2D closestTarget = null;
            float closestDistanceSquared = float.PositiveInfinity;
            foreach (Collider2D targetCollider in targetsInRadius)
            {
                // 2. 타겟의 Health 컴포넌트 확인. 
                //   - Health가 없거나, 이미 죽었거나, 자기 자신이면 무시.
                Health health = targetCollider.GetComponentInParent<Health>();
                if (health == null || health.IsDead.Value || health.transform == transform)
                    continue;

                // 3. 타겟과의 거리 계산 및 장애물 확인
                Vector2 targetCenter = targetCollider.bounds.center;
                float distanceSquared = (targetCenter - (Vector2)origin).sqrMagnitude;
                // 4. 가장 가까운 타겟 갱신
                if (distanceSquared >= closestDistanceSquared
                    || Physics2D.Linecast(origin, targetCenter, obstacleMask))
                    continue;

                closestTarget = targetCollider;
                closestDistanceSquared = distanceSquared;
            }
            // 5. 가장 가까운 타겟이 있으면 Target에 할당, 없으면 null
            Target = closestTarget != null ? closestTarget.transform : null;
            return Target != null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Target != null ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + offset, viewRadius);
        }
    }
}
