using UnityEngine;

public class Managers : MonoBehaviour
{
    public static Managers Instance { get; private set; }

    [SerializeField] private UIManager _ui;
    [SerializeField] private GameManager _game;
    [SerializeField] private GameFlowManager _flow;

    public UIManager UI => _ui;
    public GameManager Game => _game;
    public GameFlowManager Flow => _flow;
    public InventorySystem Inventory { get; private set; }
    public PlayerEquipment Equipment { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Inventory = new InventorySystem();
        Equipment = new PlayerEquipment();
    }
}
