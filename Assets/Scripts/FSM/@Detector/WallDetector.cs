namespace ProjectRE
{
using UnityEngine;

/// <summary>
/// Agent가 바라보는 방향으로 Raycast를 수행해 전방 장애물을 감지합니다.
/// </summary>
public class WallDetector : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 20)]
    [SerializeField] private float viewRange = 1f;

    /// <summary>설정된 감지 거리 안에 전방 벽 또는 장애물이 있는지 확인합니다.</summary>
    public bool IsWallInFront()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left; 
        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position, dir, viewRange, obstacleMask);
        return hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsWallInFront() ? Color.red : Color.green;
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Gizmos.DrawLine((Vector2)transform.position, (Vector2)transform.position + dir * viewRange);
    }

}
}
