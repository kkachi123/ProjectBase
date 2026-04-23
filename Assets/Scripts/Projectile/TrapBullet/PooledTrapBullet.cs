using System;
using UnityEngine;
using UnityEngine.Pool;

public class PooledTrapBullet : MonoBehaviour
{
    [SerializeField] float damage = 1f;
    IObjectPool<PooledTrapBullet> OnReleaseToPool;

    public void Initialize(IObjectPool<PooledTrapBullet> pool)
    {
        OnReleaseToPool = pool;
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet")) return;
        if (collision.CompareTag("Floor")) return;

        if(collision.TryGetComponent(out IDamageable target)) target.TakeDamage(damage);

        OnReleaseToPool.Release(this);
    }
}
