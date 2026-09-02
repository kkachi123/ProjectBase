public class PlayerManager
{
    public InventorySystem Inventory { get; } = new();
    public PlayerEquipment Equipment { get; } = new();
    public PlayerController CurrentPlayer { get; private set; }
    public PlayerInput Input { get; private set; }

    public void Register(PlayerInput input) => Input = input;
    public void Unregister(PlayerInput _) => Input = null;

    public void RegisterPlayer(PlayerController player) => CurrentPlayer = player;
    public void UnregisterPlayer(PlayerController player)
    {
        if (CurrentPlayer == player)
            CurrentPlayer = null;
    }
}
