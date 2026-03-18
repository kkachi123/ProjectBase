using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class TrapBulletController : MonoBehaviour
{
    [SerializeField] TrapBulletPoolManager _pool;
    [SerializeField] float _bulletSpeed = 5f;
    [SerializeField] Vector2 _fireDirection = Vector2.right;
    [SerializeField] float _fireInterval = 1f;
    [SerializeField] Transform[] _shootingPos;


    private void Start()
    {
        // 이 객체가 파괴되면 비동기 루프도 자동으로 멈추도록 토큰 전달
        // 안전한 비동기 처리
        ShootingBullets(this.GetCancellationTokenOnDestroy()).Forget();
    }
    private async UniTask ShootingBullets(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            int randomIndex = UnityEngine.Random.Range(0, _shootingPos.Length);
            Vector2 pos = _shootingPos[randomIndex].position;
            FireBullet(pos);
            await UniTask.Delay(TimeSpan.FromSeconds(_fireInterval) , cancellationToken : ct); 
        }
    }

    private void FireBullet(Vector2 FirePos)
    {
        _pool.FireBullet(FirePos, _fireDirection, _bulletSpeed);
    }
}
