using System;
using UnityEngine;
using UnityEngine.UI;

public class EquipSlotButton : MonoBehaviour
{
    [SerializeField] private EquipSlot _slot;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _highlight;

    private static readonly Color _emptyColor = new(0f, 0f, 0f, 0.47f);

    public EquipSlot Slot => _slot;
    public event Action<EquipSlot> OnSelected;

    private bool _initialized = false;

    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        GetComponent<Button>().onClick.AddListener(() => OnSelected?.Invoke(_slot));
    }

    public void SetEquipped(ItemData item)
    {
        EnsureInit();
        bool hasItem = item != null;
        _icon.sprite = hasItem ? item.Icon : null;
        _icon.color = hasItem ? Color.white : _emptyColor;
    }

    public void SetSelected(bool selected)
    {
        EnsureInit();
        Color color = _highlight.color;
        color.a = selected ? 1f : 0f;
        _highlight.color = color;
    }
}
