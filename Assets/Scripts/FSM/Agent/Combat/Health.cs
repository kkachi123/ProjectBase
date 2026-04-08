using System;
using UnityEngine;
using UniRx;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private FloatReactiveProperty _currentHealth = new FloatReactiveProperty();
    public IReadOnlyReactiveProperty<float> CurrentHealth => _currentHealth;
    public float MaxHealth { get; private set; }

    private BoolReactiveProperty _isDead = new BoolReactiveProperty();
    public IReadOnlyReactiveProperty<bool> IsDead => _isDead;

    private Subject<Vector2> _onKnockback = new Subject<Vector2>();
    public IObservable<Vector2> OnKnockback => _onKnockback;

    public void Initialize(float maxHealth)
    {
        _currentHealth.Value = maxHealth;
        MaxHealth = maxHealth;
        _isDead.Value = false;
    }

    public void TakeDamage(float damageAmount, Vector2 attackerPos)
    {
        if (_isDead.Value) return;
        _currentHealth.Value = Mathf.Max(_currentHealth.Value - damageAmount, 0);
        Vector2 kockbackDir = ((Vector2)transform.position - attackerPos).normalized;
        _onKnockback.OnNext(kockbackDir);
        if (_currentHealth.Value <= 0) _isDead.Value = true;
    }
}