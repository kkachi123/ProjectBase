using UnityEngine;
using System;

[Serializable]
public class AgentCombatHandler : MonoBehaviour
{
    [SerializeField] private BoxCollider2D[] _attackAreas; 
    [SerializeField] private LayerMask _targetLayer;
    private float _attackDamage = 0f;
    public int CurrentAttackType { get; private set; }

    public void Initialize(float attackDamage)
    {
        _attackDamage = attackDamage;
    }

    public void SetAttackType(int attackType)
    {
        CurrentAttackType = attackType;
    }

    public void PerformAttack()
    {
        BoxCollider2D _attackArea = _attackAreas[CurrentAttackType - 1];
        Collider2D[] hitTargets = Physics2D.OverlapBoxAll(_attackArea.bounds.center, _attackArea.bounds.size, 0f, _targetLayer);
        foreach (Collider2D target in hitTargets)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_attackDamage);
            }
        }
    }
}
