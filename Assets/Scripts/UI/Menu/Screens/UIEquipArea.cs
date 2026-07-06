using System;
using System.Collections.Generic;
using UnityEngine;

public class UIEquipArea : MonoBehaviour
{
    [SerializeField] private Transform _slotsContent;

    private InventorySlot[] _slots;

    public event Action<ItemData> OnItemSelected;

    private void Awake()
    {
        _slots = new InventorySlot[_slotsContent.childCount];
        for (int i = 0; i < _slotsContent.childCount; i++)
        {
            _slots[i] = _slotsContent.GetChild(i).GetComponent<InventorySlot>();
            _slots[i].OnClicked += item => OnItemSelected?.Invoke(item);
        }
    }

    public void Render(IReadOnlyList<ItemData> items)
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i].Set(i < items.Count ? items[i] : null);
    }
}
