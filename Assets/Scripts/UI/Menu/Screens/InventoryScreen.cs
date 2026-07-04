using System.Collections.Generic;

public class InventoryScreen : UITab
{
    private InventorySystem _inventory;

    public override void OnShow()
    {
        base.OnShow();
        _inventory = Managers.Instance.Player.Inventory;
        _inventory.OnChanged += Render;
        Render(_inventory.GetItems());
    }

    public override void OnHide()
    {
        base.OnHide();
        _inventory.OnChanged -= Render;
        _inventory = null;
    }

    private void Render(IReadOnlyList<ItemData> items)
    {
        // TODO: 아이템 목록 UI 갱신
    }
}
