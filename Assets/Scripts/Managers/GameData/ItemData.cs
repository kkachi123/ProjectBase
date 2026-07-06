using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemData")]
public class ItemData : ScriptableObject
{
    public string Id;
    public string Name;
    public Sprite Icon;
    public EquipSlot? EquipSlot;
}
