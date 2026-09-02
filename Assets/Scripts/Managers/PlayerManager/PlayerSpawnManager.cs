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
        Managers.Instance.Player.RegisterPlayer(CurrentPlayer);
        return CurrentPlayer;
    }

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
