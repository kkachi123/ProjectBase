using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _item;

    public void Interact()
    {
        if (_item == null) return;
        // item
        if(_item.EquipSlot == EquipSlot.None) 
            Managers.Instance.Player.Inventory.Add(_item);
        // equipment
        else
            Managers.Instance.Player.Equipment.Add(_item);
        Destroy(gameObject);
    }
}
