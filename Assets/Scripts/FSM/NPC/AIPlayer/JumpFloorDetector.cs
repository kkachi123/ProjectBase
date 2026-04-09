using UnityEngine;

public class JumpFloorDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 20)]
    [SerializeField] private float viewRadius = 5f;
    [SerializeField] private float canJumpWidth = 0.6f;

    private Vector2 closestGroundPos;
    private float vX , vY , gravity;

    public void SetJumpParameters(float vX, float vY, float gravityValue)
    {
        this.vX = vX;
        this.vY = vY;
        gravity = gravityValue;
    }

    private bool IsJumpReachable(float diffX, float diffY, float vX, float vY, float gravity, out float timeToLand)
    {
        timeToLand = 0f;

        // 1. 점프 최고 높이(maxHeight) 체크
        float maxHeight = (vY * vY) / (2f * gravity);
        if (diffY > maxHeight * 0.95f) return false; // 여유값 5%

        // 2. 근의 공식을 이용한 체공 시간 계산
        // 공식: 0.5 * g * t^2 - vY * t + diffY = 0
        float a = 0.5f * gravity;
        float b = -vY;
        float c = diffY;
        float determinant = b * b - 4f * a * c;

        // 판별식이 음수면 해당 높이에 물리적으로 도달 불가
        if (determinant < 0) return false;

        // 하강 중에 착지하는 시간(큰 근)을 선택
        timeToLand = (-b + Mathf.Sqrt(determinant)) / (2f * a);

        // 3. 해당 시간 동안 이동 가능한 최대 수평 거리 체크
        float maxJumpWidth = vX * timeToLand;

        return diffX <= maxJumpWidth;
    }
    public Transform GetClosedGround()
    {
        Collider2D[] groundsInRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, _groundMask);
        Transform closestGround = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D groundCollider in groundsInRadius)
        {
            Vector2 startPos = transform.position;
            Vector2 targetPoint = groundCollider.ClosestPoint(startPos);

            float diffY = targetPoint.y - startPos.y;
            float diffX = Mathf.Abs(targetPoint.x - startPos.x);

            // 기본 필터링 : 아래층이거나 x값이 너무 붙은 경우(천장에 부딪힘)
            // 장애물 체크 (Linecast)
            if (diffY < 0 || diffX < canJumpWidth) continue;
            if (Physics2D.Linecast(transform.position, targetPoint, obstacleMask)) continue;
            
            // Jump Possibility Check 
            if (IsJumpReachable(diffX, diffY, vX, vY, gravity, out float t))
            {
                // 거리 비교 후 최적의 지면 선택
                float dist = Vector2.Distance(startPos, targetPoint);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestGround = groundCollider.transform;
                    closestGroundPos = targetPoint; // Gizmo visualization
                }
            }
        }
        return closestGround;
    }

    private void OnDrawGizmos()
    {
        // Detection Radius
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Preview of jumpable grounds 
        if (Application.isPlaying)
        {
            if (GetClosedGround() != null)
            {
                // Landing point Line
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, closestGroundPos);

                // Landing point Box & Sphere
                Gizmos.DrawWireCube(closestGroundPos, Vector3.one * 0.2f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(closestGroundPos, 0.1f);
            }
        }
    }
}
