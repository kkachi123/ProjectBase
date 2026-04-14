using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 20)]
    [SerializeField] private float viewRadius = 5f;

    public Transform Target { get; private set; }

    public bool IsTargetInView()
    {
        Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, playerMask);
        foreach (Collider2D targetCollider in targetsInRadius)
        {
            Transform target = targetCollider.transform;
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            // Check Wall Obstacle (Linecast)
            if (!Physics2D.Linecast(transform.position, target.position, obstacleMask))
            {
                Target = target;
                return true;
            }
        }
        Target = null;
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsTargetInView() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
}
