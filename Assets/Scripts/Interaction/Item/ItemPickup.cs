using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _item;

    public void Interact()
    {
        if (_item == null) return;
        Managers.Instance.Player.Inventory.Add(_item);
        Destroy(gameObject);
    }
}
