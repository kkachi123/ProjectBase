using System.Collections.Generic;

public class EquipmentScreen : UITab
{
    private PlayerEquipment _equipment;

    public override void OnShow()
    {
        base.OnShow();
        _equipment = Managers.Instance.Equipment;
        _equipment.OnChanged += Render;
        Render(_equipment.GetSlots());
    }

    public override void OnHide()
    {
        base.OnHide();
        _equipment.OnChanged -= Render;
        _equipment = null;
    }

    private void Render(IReadOnlyDictionary<EquipSlot, ItemData> slots)
    {
        // TODO: 장비 슬롯 UI 갱신
    }
}
