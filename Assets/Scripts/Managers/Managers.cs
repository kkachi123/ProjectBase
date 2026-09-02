using UnityEngine;
using UnityEngine.Serialization;

public class Managers : MonoBehaviour
{
    public static Managers Instance { get; private set; }

    [SerializeField] private UIManager _ui;
    [SerializeField] private GameManager _game;
    [FormerlySerializedAs("_flow")]
    [SerializeField] private SceneFlowManager _sceneFlow;
    [SerializeField] private MapManager _map;
    [SerializeField] private CameraManager _camera;
    [SerializeField] private LightingManager _lighting;
    [SerializeField] private AdventureRunManager _adventureRun;

    public UIManager UI => _ui;
    public GameManager Game => _game;
    public SceneFlowManager SceneFlow => _sceneFlow;
    public MapManager Map => _map;
    public CameraManager Camera => _camera;
    public LightingManager Lighting => _lighting;
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
