namespace ProjectRE
{
using UnityEngine;

/// <summary>data에 정의된 범위로 2D 근접 공격 처리.+ 범위 표시(Gizmos).</summary>
public class AgentMeleeHitHandler2D : MonoBehaviour
{
    [SerializeField] private LayerMask _targetLayer;
    private AttackData? _previewData;

    /// <summary>시선 방향을 반영한 실제 타격·Editor 공용 범위 계산.</summary>
    public void GetHitBox(AttackData data, out Vector2 center, out Vector2 size)
    {
        center = (Vector2)transform.position + new Vector2(data.offset.x * (transform.localScale.x > 0 ? 1 : -1), data.offset.y);
        size = data.size;
    }

    /// <summary>호출 시 전달된 데이터로 피해·넉백 적용.</summary>
    public void PerformAttack(AttackData data)
    {
        GetHitBox(data, out Vector2 center, out Vector2 size);
        Collider2D[] hitTargets = Physics2D.OverlapBoxAll(center, size, 0f, _targetLayer);
        foreach (Collider2D target in hitTargets)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(data.damage);
            }
            if(target.TryGetComponent(out IKnockbackListener knockbackListener))
            {
                Vector2 knockbackDir = (target.transform.position - transform.position).normalized;
                knockbackListener.HandleKnockback(knockbackDir);
            }
        }
    }
    /// <summary>실행 선택과 무관한 Gizmo 표시 데이터 설정.</summary>
    public void SetPreviewData(AttackData data) => _previewData = data;

    /// <summary>공격 종료·중단 시 범위 표시 해제.</summary>
    public void ClearPreview() => _previewData = null;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_previewData.HasValue) return;
        GetHitBox(_previewData.Value, out Vector2 center, out Vector2 size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
}
