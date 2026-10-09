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

        /// <summary>감지 반경 내 후보와 탐색 원점 반환.</summary>
        public Collider2D[] FindCandidates(out Vector2 origin)
        {
            origin = transform.position + offset;
            return Physics2D.OverlapCircleAll(origin, viewRadius, playerMask);
        }

        /// <summary>탐색 원점과 후보 중심 사이 장애물 확인.</summary>
        public bool HasLineOfSight(Vector2 origin, Collider2D candidate)
        {
            return !Physics2D.Linecast(origin, candidate.bounds.center, obstacleMask);
        }

        /// <summary>탐색 원점과 반경 표시.</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + offset, viewRadius);
        }
    }
}
