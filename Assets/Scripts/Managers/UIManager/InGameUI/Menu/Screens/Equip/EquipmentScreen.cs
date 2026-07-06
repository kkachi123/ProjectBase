using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentScreen : UITab
{
    [SerializeField] private EquipSlotButton[] _slotButtons;
    [SerializeField] private UIEquipArea _equipArea;

    private PlayerEquipment _equipment;
    private InventorySystem _inventory;
    private EquipSlot _selectedSlot;

    private void Awake()
    {
        foreach (var btn in _slotButtons)
            btn.OnSelected += SelectSlot;

        _equipArea.OnItemSelected += OnEquipItem;
    }

    public override void OnShow()
    {
        base.OnShow();
        _equipment = Managers.Instance.Player.Equipment;
        _inventory = Managers.Instance.Player.Inventory;
        _equipment.OnChanged += RenderEquipped;
        RenderEquipped(_equipment.GetSlots());
        SelectSlot(_slotButtons[0].Slot);
    }

    public override void OnHide()
    {
        base.OnHide();
        _equipment.OnChanged -= RenderEquipped;
        _equipment = null;
        _inventory = null;
    }

    private void SelectSlot(EquipSlot slot)
    {
        _selectedSlot = slot;
        foreach (var btn in _slotButtons)
            btn.SetSelected(btn.Slot == slot);

        var filtered = _inventory.GetItems()
            .Where(item => item.EquipSlot == slot)
            .ToList();
        _equipArea.Render(filtered);
    }

    private void RenderEquipped(IReadOnlyDictionary<EquipSlot, ItemData> slots)
    {
        foreach (var btn in _slotButtons)
            btn.SetEquipped(slots.GetValueOrDefault(btn.Slot));
    }

    private void OnEquipItem(ItemData item)
    {
        _equipment.Equip(_selectedSlot, item);
        SelectSlot(_selectedSlot);
    }
}
