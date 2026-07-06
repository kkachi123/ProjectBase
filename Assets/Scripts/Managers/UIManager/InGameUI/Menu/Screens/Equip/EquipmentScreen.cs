using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentScreen : UITab
{
    [SerializeField] private EquipSlotButton[] _slotButtons;
    [SerializeField] private UIEquipArea _equipArea;

    private PlayerEquipment _equipment;
    private EquipSlot _selectedSlot;
    private bool _initialized;

    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var btn in _slotButtons) btn.OnSelected += SelectSlot;
        _equipArea.OnItemSelected += OnEquipItem;
    }

    public override void OnShow()
    {
        base.OnShow();
        EnsureInit();
        _equipment = Managers.Instance.Player.Equipment;
        _equipment.OnChanged += RenderEquipped;
        RenderEquipped(_equipment.GetEquipped());
    }

    public override void OnHide()
    {
        base.OnHide();
        _equipArea.Hide();
        _equipment.OnChanged -= RenderEquipped;
        _equipment = null;
    }

    private void SelectSlot(EquipSlot slot)
    {
        _selectedSlot = slot;
        foreach (var btn in _slotButtons)
            btn.SetSelected(btn.Slot == slot);

        var filtered = _equipment.GetGear()
            .Where(item => item.EquipSlot == slot)
            .ToList();
        _equipArea.Render(filtered);
    }

    private void RenderEquipped(IReadOnlyDictionary<EquipSlot, ItemData> equipped)
    {
        foreach (var btn in _slotButtons)
            btn.SetEquipped(equipped.GetValueOrDefault(btn.Slot));
    }

    private void OnEquipItem(ItemData item)
    {
        _equipment.Equip(_selectedSlot, item);
    }
}
