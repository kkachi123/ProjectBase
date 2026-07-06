public class PlayerManager
{
    public InventorySystem Inventory { get; } = new();
    public PlayerEquipment Equipment { get; } = new();
    public PlayerInput Input { get; private set; }

    public void Register(PlayerInput input) => Input = input;
    public void Unregister(PlayerInput _) => Input = null;
}
