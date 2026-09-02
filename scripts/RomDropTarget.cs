using Godot;

// Gắn script này lên Panel dùng làm "khe thả ROM" bên trong RomSlotUI.tscn.
// Nhận đúng kiểu dữ liệu drag mà InventorySlot._GetDragData() trả về (int = SlotIndex).
public partial class RomDropTarget : Panel
{
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType != Variant.Type.Int) return false;

		int slotIndex = data.AsInt32();
		var slots = Inventory.Instance?.Slots;
		if (slots == null || slotIndex < 0 || slotIndex >= slots.Count) return false;

		var slot = slots[slotIndex];
		if (slot == null || slot.IsEmpty) return false;

		// Chỉ cho thả nếu item đúng là 1 trong các ROM hợp lệ của trạm đang mở
		return RomSlotUI.Instance != null && RomSlotUI.Instance.IsValidRom(slot.Item);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		int fromIndex = data.AsInt32();
		RomSlotUI.Instance?.TryHandleDrop(fromIndex);
	}
}
