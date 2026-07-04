using System;
using System.Collections.Generic;

public class InventorySystem
{
    private readonly List<ItemData> _items = new();

    public event Action<IReadOnlyList<ItemData>> OnChanged;
    public event Action<ItemData> OnItemAdded;

    public IReadOnlyList<ItemData> GetItems() => _items;

    public void Add(ItemData item)
    {
        _items.Add(item);
        OnItemAdded?.Invoke(item);
        OnChanged?.Invoke(_items);
    }

    public bool Remove(ItemData item)
    {
        bool removed = _items.Remove(item);
        if (removed) OnChanged?.Invoke(_items);
        return removed;
    }
}
