using UnityEngine;

public class WallDetector : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [Range(0, 20)]
    [SerializeField] private float viewRange = 1f;

    public bool IsWallInFront()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left; 
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, viewRange, obstacleMask);
        return hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsWallInFront() ? Color.red : Color.green;
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dir * viewRange);
    }

}
