using System.Collections.Generic;
using UnityEngine;

public class InventoryScreen : UITab
{
    [SerializeField] private Transform _slotsContent;
    [SerializeField] private UIItemDetailPanel _detailPanel;

    private InventorySystem _inventory;
    private InventorySlot[] _slots;

    private void EnsureInit()
    {
        if (_slots != null) return;
        _slots = new InventorySlot[_slotsContent.childCount];
        for (int i = 0; i < _slotsContent.childCount; i++)
        {
            _slots[i] = _slotsContent.GetChild(i).GetComponent<InventorySlot>();
            _slots[i].OnClicked += _detailPanel.Show;
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        EnsureInit();
        _inventory = Managers.Instance.Player.Inventory;
        _inventory.OnChanged += Render;
        Render(_inventory.GetItems());
    }

    public override void OnHide()
    {
        base.OnHide();
        _detailPanel.Hide();
        _inventory.OnChanged -= Render;
        _inventory = null;
    }

    private void Render(IReadOnlyList<ItemData> items)
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i].Set(i < items.Count ? items[i] : null);
    }
}
