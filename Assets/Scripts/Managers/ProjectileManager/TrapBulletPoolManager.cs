using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class TrapBulletPoolManager : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private TrapBulletFactory _trapBulletFactory;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private int _maxPoolSize = 30;
    IObjectPool<PooledTrapBullet> _bulletPool;

    private void Awake()
    {
        _bulletPool = new ObjectPool<PooledTrapBullet>
            (
                CreateBullet, 
                OnGetBullet, 
                OnReleaseBullet,
                OnDestroyBullet, 
                true, _initialPoolSize, _maxPoolSize
            );
    }

    private PooledTrapBullet CreateBullet()
    {
        GameObject obj = _trapBulletFactory.CreateTrapBullet();
        PooledTrapBullet bullet = obj.GetComponent<PooledTrapBullet>();
        bullet.Initialize(_bulletPool);

        return bullet;
    }

    private void OnGetBullet(PooledTrapBullet bullet)
    {
        bullet.gameObject.SetActive(true);
    }

    private void OnReleaseBullet(PooledTrapBullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyBullet(PooledTrapBullet bullet)
    {
        Destroy(bullet.gameObject);
    }

    public void FireBullet (Vector2 position, Vector2 direction, float speed)
    {
        PooledTrapBullet bullet = _bulletPool.Get();
        bullet.transform.position = position;
        bullet.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;
    }
}
