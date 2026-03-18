//using System;
using UnityEngine;
using UniRx;

public class Health : MonoBehaviour , IDamageable
{
    // UI나 애니메이터가 구독할 수 있는 이벤트
    // ReactiveProperty<float> 도 가능하지만
    // Generic 은 Inspector 에서 보이지 않으므로 FloatReactiveProperty로 선언
    // IReadOnlyReactiveProperty<float>로 노출하여 외부에서 값을 변경하지 못하도록 함
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
