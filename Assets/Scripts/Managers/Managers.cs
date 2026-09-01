using UnityEngine;

public class Managers : MonoBehaviour
{
    public static Managers Instance { get; private set; }

    [SerializeField] private UIManager _ui;
    [SerializeField] private GameManager _game;
    [SerializeField] private GameFlowManager _flow;
    [SerializeField] private MapManager _map;
    [SerializeField] private AdventureRunManager _adventureRun;

    public UIManager UI => _ui;
    public GameManager Game => _game;
    public GameFlowManager Flow => _flow;
    public MapManager Map => _map;
    public AdventureRunManager AdventureRun => _adventureRun;
    public PlayerManager Player { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Player = new PlayerManager();
    }
}
