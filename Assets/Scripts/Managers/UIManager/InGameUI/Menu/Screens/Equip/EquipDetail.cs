using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipDetail : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void Show(ItemData item)
    {
        _icon.sprite = item.Icon;
        _nameText.text = item.Name;
        _descriptionText.text = item.Description;
        _panel.SetActive(true);
    }

    public void Hide() => _panel.SetActive(false);
}
