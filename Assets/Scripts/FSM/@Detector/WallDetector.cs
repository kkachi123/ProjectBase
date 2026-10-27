namespace ProjectRE
{
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몸통 Collider 형상과 접촉 법선으로 전방 장애물 감지.
/// </summary>
public class WallDetector : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("몸통 Collider 외곽에서 전방으로 검사할 거리")]
    [Range(0, 20)]
    [SerializeField] private float viewRange = 1f;

    private Collider2D _bodyCollider;
    private ContactFilter2D _contactFilter;
    private readonly List<RaycastHit2D> _castHits = new(4);
    private readonly List<ContactPoint2D> _contacts = new(4);

    /// <summary>몸통 참조·장애물 필터 캐시. Trigger 제외.</summary>
    private void Awake()
    {
        _bodyCollider = GetComponent<Collider2D>();
        _contactFilter.SetLayerMask(obstacleMask);
        _contactFilter.useTriggers = false;
    }

    /// <summary>몸통 전체 높이에서 진행 방향을 막는 벽 확인.</summary>
    public bool IsWallInFront()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        int contactCount = _bodyCollider.GetContacts(_contactFilter, _contacts);
        for (int i = 0; i < contactCount; i++)
            if (IsBlockingNormal(_contacts[i].normal, direction))
                return true;

        int hitCount = _bodyCollider.Cast(direction, _contactFilter, _castHits, viewRange);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _castHits[i];
            // 시작 겹침의 역방향 법선은 제외. 실제 접촉은 위에서 판정.
            if (hit.distance > 0f && IsBlockingNormal(hit.normal, direction))
                return true;
        }
        return false;
    }

    /// <summary>바닥·천장·이동 반대편 접촉을 제외한 측면 충돌 판정.</summary>
    private static bool IsBlockingNormal(Vector2 normal, Vector2 direction)
    {
        return Vector2.Dot(normal, direction) < 0f
            && Mathf.Abs(normal.x) > Mathf.Abs(normal.y);
    }

    /// <summary>몸통 Bounds와 전방 이동 거리를 포함한 감지 외곽 표시.</summary>
    private void OnDrawGizmos()
    {
        Collider2D body = _bodyCollider;
        // 편집 모드에서는 Awake 이전이므로 Gizmo용 참조만 확보.
        if (body == null && !TryGetComponent(out body))
            return;

        Vector3 direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        Bounds bounds = body.bounds;
        Gizmos.color = Application.isPlaying && _bodyCollider != null && IsWallInFront()
            ? Color.red : Color.green;
        Gizmos.DrawWireCube(bounds.center + direction * (viewRange * 0.5f),
            bounds.size + Vector3.right * viewRange);
    }

}
}
