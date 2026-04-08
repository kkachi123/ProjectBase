using UnityEngine;

public class JumpFloorDetector : MonoBehaviour
{
    [SerializeField] Transform eyePosition;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 20)]
    [SerializeField] private float viewRadius = 5f;
    [SerializeField] private float canJumpWidth = 0.6f;

    public Transform currentTarget;
    private Vector2 closestGroundPos;

    private bool IsJumpReachable(float diffX, float diffY, float vX, float vY, float gravity, out float timeToLand)
    {
        timeToLand = 0f;

        // 1. 점프 최고 높이(Peak) 체크
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
        Collider2D[] groundsInRadius = Physics2D.OverlapCircleAll(eyePosition.position, viewRadius, _groundMask);
        Transform closestGround = null;
        float closestDistance = Mathf.Infinity;

        // 데이터 로드
        float vY = 8f;
        float vX = 5f;
        float gravity = Mathf.Abs(Physics2D.gravity.y);

        foreach (Collider2D groundCollider in groundsInRadius)
        {
            Vector2 startPos = transform.position;
            Vector2 targetPoint = groundCollider.ClosestPoint(startPos);

            float diffY = targetPoint.y - startPos.y;
            float diffX = Mathf.Abs(targetPoint.x - startPos.x);

            // 기본 필터링 (아래에 있거나 장애물에 막힌 경우)
            if (diffY < -0.2f || diffX < canJumpWidth) continue; 
            if (Physics2D.Linecast(eyePosition.position, targetPoint, obstacleMask)) continue;

            // 분리한 함수 호출
            if (IsJumpReachable(diffX, diffY, vX, vY, gravity, out float t))
            {
                // 거리 비교 후 최적의 지면 선택
                float dist = Vector2.Distance(startPos, targetPoint);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestGround = groundCollider.transform;
                    closestGroundPos = targetPoint; // 착지 예상 위치 저장 (필요 시 활용)
                }
            }
        }
        currentTarget = closestGround;
        return closestGround;
    }

    //에디터에서 시야 범위를 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(eyePosition.position, viewRadius);

        if (Application.isPlaying)
        {
            Transform ground = GetClosedGround();
            
            if (ground != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, closestGroundPos);
                Gizmos.DrawWireCube(closestGroundPos, Vector3.one * 0.2f);

                // 예상 궤적(간이)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(closestGroundPos, 0.1f);
            }
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, 2.0f, obstacleMask);
        if (hit.collider != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.2f);
        }
    }

}
