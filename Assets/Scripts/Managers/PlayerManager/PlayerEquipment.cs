using System;
using System.Collections.Generic;

public enum EquipSlot { None, Weapon, Armor, Accessory }

public class PlayerEquipment
{
    // 장착된 장비 아이템 목록
    private readonly Dictionary<EquipSlot, ItemData> _equipped = new();
    // 소유한 장비 아이템 목록
    private readonly List<ItemData> _gear = new();
    // 장착된 장비 아이템이 변경될 때 발생하는 이벤트

    public event Action<IReadOnlyDictionary<EquipSlot, ItemData>> OnChanged;

    public IReadOnlyDictionary<EquipSlot, ItemData> GetEquipped() => _equipped;
    public IReadOnlyList<ItemData> GetGear() => _gear;
    public ItemData GetEquipped(EquipSlot slot) => _equipped.GetValueOrDefault(slot);

    public void Add(ItemData item)
    {
        _gear.Add(item);
    }

    public void Equip(EquipSlot slot, ItemData item)
    {
        if (item.EquipSlot != slot) return;
        _equipped[slot] = item;
        OnChanged?.Invoke(_equipped);
    }

    public void Unequip(EquipSlot slot)
    {
        if (_equipped.Remove(slot))
            OnChanged?.Invoke(_equipped);
    }
}
