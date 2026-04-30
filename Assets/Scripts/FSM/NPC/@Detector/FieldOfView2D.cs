using UnityEngine;

public class FieldOfView2D : MonoBehaviour
{
    [SerializeField] Transform eyePosition;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 360)]
    [SerializeField] private float viewAngle = 90f;
    [Range(0, 20)]
    [SerializeField] private float viewRadius = 5f;
    public Transform currentTarget;

    public bool IsTargetInView()
    {
        // 1. 반경 내의 모든 타겟 탐색
        Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(eyePosition.position, viewRadius, _targetMask);

        foreach (Collider2D targetCollider in targetsInRadius)
        {
            Transform target = targetCollider.transform;
            Vector2 dirToTarget = (target.position - eyePosition.position).normalized;
            Vector2 lookDir = transform.localScale.x < 0 ? Vector2.left : Vector2.right;

            if (Vector2.Angle(lookDir, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector2.Distance(eyePosition.position, target.position);

                // 3. 장애물(벽)에 가려져 있는지 체크 (Linecast)
                if (!Physics2D.Linecast(eyePosition.position, target.position, obstacleMask))
                {
                    currentTarget = target;
                    return true;
                }
            }
        }

        currentTarget = null;
        return false;
    }

    //에디터에서 시야 범위를 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(eyePosition.position, viewRadius);
        Vector3 leftBoundary = DirFromAngle(-viewAngle / 2);
        Vector3 rightBoundary = DirFromAngle(viewAngle / 2);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightBoundary * viewRadius);
    }

    private Vector3 DirFromAngle(float angleInDegrees)
    {
        // 1. 에이전트의 현재 스케일이나 로직상의 방향을 확인
        // x scale이 -1이면 기본 방향(오른쪽)에 180도를 더해줍니다.
        float facingRotation = transform.localScale.x < 0 ? 180f : 0f;

        // 2. 전체 회전값에 facingRotation을 더함
        angleInDegrees += eyePosition.eulerAngles.z + facingRotation;

        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0);
    }
}
