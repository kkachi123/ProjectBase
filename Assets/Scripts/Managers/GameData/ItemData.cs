using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemData")]
public class ItemData : ScriptableObject
{
    public string Id;
    public string Name;
    [TextArea] public string Description;
    public Sprite Icon;
    public EquipSlot? EquipSlot;
}
