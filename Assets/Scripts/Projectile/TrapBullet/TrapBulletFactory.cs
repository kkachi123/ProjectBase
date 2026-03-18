using UnityEngine;

public class TrapBulletFactory : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;

    public GameObject CreateTrapBullet()
    {
        return Instantiate(_bulletPrefab);
    }
}
