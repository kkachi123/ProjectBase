using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    private static readonly Color _emptyColor = new(0f, 0f, 0f, 0.47f);

    public event Action<ItemData> OnClicked;

    private Image _icon;
    private Button _button;
    private ItemData _item;

    private void EnsureInit()
    {
        if (_button != null) return;
        _icon = GetComponent<Image>();
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    public void Set(ItemData item)
    {
        EnsureInit();
        _item = item;
        bool hasItem = item != null;
        _icon.sprite = hasItem ? item.Icon : null;
        _icon.color = hasItem ? Color.white : _emptyColor;
        _button.interactable = hasItem;
    }

    private void OnClick() => OnClicked?.Invoke(_item);
}
