using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIEquipArea : MonoBehaviour
{
    [SerializeField] private GameObject _slotsContent;
    [SerializeField] private Button _cancelButton;

    private InventorySlot[] _slots;

    public event Action<ItemData> OnItemSelected;

    private void EnsureInit()
    {
        if (_slots != null) return;
        Transform slotsTransform = _slotsContent.transform;
        _slots = new InventorySlot[slotsTransform.childCount];
        for (int i = 0; i < slotsTransform.childCount; i++)
        {
            _slots[i] = slotsTransform.GetChild(i).GetComponent<InventorySlot>();
            _slots[i].OnClicked += item => OnItemSelected?.Invoke(item);
            _slots[i].OnClicked += _ => Hide();
        }
        _cancelButton.onClick.AddListener(Hide);
    }
    public void Show()
    {
        _slotsContent.SetActive(true);
        _cancelButton.gameObject.SetActive(true);
    }

    public void Hide() 
    {
        _slotsContent.SetActive(false);
        _cancelButton.gameObject.SetActive(false);
    }

    public void Render(IReadOnlyList<ItemData> items)
    {
        EnsureInit();
        Show();
        for (int i = 0; i < _slots.Length; i++) _slots[i].Set(i < items.Count ? items[i] : null);
    }
}
