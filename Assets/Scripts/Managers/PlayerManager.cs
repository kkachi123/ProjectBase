public class PlayerManager
{
    public InventorySystem Inventory { get; } = new();
    public PlayerEquipment Equipment { get; } = new();
}
