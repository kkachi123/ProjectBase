using UnityEngine;
using System.Collections.Generic;

public class AgentCombatHandler : MonoBehaviour
{
    [SerializeField] private LayerMask _targetLayer;
    private List<AttackData> _datas;
    public int CurrentAttackType { get; private set; }

    public void Initialize(List<AttackData> data)
    {
        _datas = data;
    }

    public void SetAttackType(int attackType)
    {
        CurrentAttackType = attackType;
    }

    private Vector2 CalcAreaPos(Vector2 offset) => (Vector2)transform.position + new Vector2(offset.x * (transform.localScale.x > 0 ? 1 : -1), offset.y);


    public void PerformAttack()
    {
        if (CurrentAttackType <= 0 || CurrentAttackType > _datas.Count) return;

        AttackData currentData = _datas[CurrentAttackType - 1];
        Vector2 areaPos = CalcAreaPos(currentData.offset);

        Collider2D[] hitTargets = Physics2D.OverlapBoxAll(areaPos, currentData.size, 0f, _targetLayer);
        foreach (Collider2D target in hitTargets)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(currentData.damage);
            }
            if(target.TryGetComponent(out IKnockbackListener knockbackListener))
            {
                Vector2 knockbackDir = (target.transform.position - transform.position).normalized;
                knockbackListener.HandleKnockback(knockbackDir);
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (CurrentAttackType <= 0 || CurrentAttackType > _datas.Count) return;
            AttackData currentData = _datas[CurrentAttackType - 1];
            Vector2 areaPos = CalcAreaPos(currentData.offset);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(areaPos, currentData.size);
        }
    }
}
