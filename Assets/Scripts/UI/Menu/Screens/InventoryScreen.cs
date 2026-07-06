using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryScreen : UITab
{
    [SerializeField] private Transform _slotsContent;

    private InventorySystem _inventory;
    private Image[] _slotImages;

    private static readonly Color _emptySlotColor = new(0f, 0f, 0f, 0.47f);

    private void Awake()
    {
        _slotImages = new Image[_slotsContent.childCount];
        for (int i = 0; i < _slotsContent.childCount; i++)
            _slotImages[i] = _slotsContent.GetChild(i).GetComponent<Image>();
    }

    public override void OnShow()
    {
        base.OnShow();
        _inventory = Managers.Instance.Player.Inventory;
        _inventory.OnChanged += Render;
        Render(_inventory.GetItems());
    }

    public override void OnHide()
    {
        base.OnHide();
        _inventory.OnChanged -= Render;
        _inventory = null;
    }

    private void Render(IReadOnlyList<ItemData> items)
    {
        for (int i = 0; i < _slotImages.Length; i++)
        {
            bool hasItem = i < items.Count;
            _slotImages[i].sprite = hasItem ? items[i].Icon : null;
            _slotImages[i].color = hasItem ? Color.white : _emptySlotColor;
        }
    }
}
