//using System;
using UnityEngine;
using UniRx;

public class Health : MonoBehaviour , IDamageable
{
    private FloatReactiveProperty _currentHealth = new FloatReactiveProperty();
    public IReadOnlyReactiveProperty<float> CurrentHealth => _currentHealth;
    public float MaxHealth { get; private set; }

    private BoolReactiveProperty _isDead = new BoolReactiveProperty();
    public IReadOnlyReactiveProperty<bool> IsDead => _isDead;

    public void Initialize(float maxHealth)
    {
        _currentHealth.Value = maxHealth;
        MaxHealth = maxHealth;
        _isDead.Value = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if(_isDead.Value) return; 
        _currentHealth.Value = Mathf.Max(_currentHealth.Value - damageAmount, 0);
        if (_currentHealth.Value <= 0) _isDead.Value = true;
    }

}
