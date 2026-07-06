using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;

    private static readonly Color _emptyColor = new(0f, 0f, 0f, 0.47f);

    public event Action<ItemData> OnClicked;

    private void Awake()
    {
        _icon = GetComponent<Image>();
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private ItemData _item;

    public void Set(ItemData item)
    {
        _item = item;
        bool hasItem = item != null;
        _icon.sprite = hasItem ? item.Icon : null;
        _icon.color = hasItem ? Color.white : _emptyColor;
        _button.interactable = hasItem;
    }

    private void OnClick() => OnClicked?.Invoke(_item);
}
