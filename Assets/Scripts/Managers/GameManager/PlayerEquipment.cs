using System;
using System.Collections.Generic;

public enum EquipSlot { Weapon, Armor, Accessory }

public class PlayerEquipment
{
    private readonly Dictionary<EquipSlot, ItemData> _slots = new();

    public event Action<IReadOnlyDictionary<EquipSlot, ItemData>> OnChanged;

    public IReadOnlyDictionary<EquipSlot, ItemData> GetSlots() => _slots;

    public ItemData GetSlot(EquipSlot slot) => _slots.GetValueOrDefault(slot);

    public void Equip(EquipSlot slot, ItemData item)
    {
        _slots[slot] = item;
        OnChanged?.Invoke(_slots);
    }

    public void Unequip(EquipSlot slot)
    {
        if (_slots.Remove(slot))
            OnChanged?.Invoke(_slots);
    }
}
