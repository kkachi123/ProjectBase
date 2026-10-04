namespace ProjectRE
{
using UnityEngine;

/// <summary>
/// Agent 시선 방향 Raycast로 전방 장애물 감지.
/// </summary>
public class WallDetector : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 20)]
    [SerializeField] private float viewRange = 1f;

    /// <summary>감지 거리 내 전방 벽·장애물 존재 여부 확인.</summary>
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
