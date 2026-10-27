namespace ProjectRE
{
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private PlayerController _playerPrefab;
    [SerializeField] private Transform _playerParent;

    public PlayerController CurrentPlayer { get; private set; }

    public PlayerController Spawn(Vector3 position)
    {
        ClearCurrentPlayer();

        if (_playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawnManager] Player prefab is missing.");
            return null;
        }

        CurrentPlayer = Instantiate(_playerPrefab, position, Quaternion.identity, _playerParent);
        return CurrentPlayer;
    }

    // 최초 상태 진입을 마친 Player 등록.
    public void RegisterCurrentPlayer() => Managers.Instance.Player.RegisterPlayer(CurrentPlayer);

    public void MoveCurrentPlayer(Vector3 position)
    {
        if (CurrentPlayer == null) return;
        CurrentPlayer.transform.position = position;
    }

    public void ClearCurrentPlayer()
    {
        if (CurrentPlayer == null) return;

        Managers.Instance.Player.UnregisterPlayer(CurrentPlayer);
        Destroy(CurrentPlayer.gameObject);
        CurrentPlayer = null;
    }
}
}
