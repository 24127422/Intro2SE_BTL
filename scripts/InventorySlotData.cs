// 1 ô trong túi đồ
public class InventorySlotData
{
	public Item Item { get; set; } = null;
	public int Quantity { get; set; } = 0;

	// Trạng thái RIÊNG của từng ô — KHÔNG lưu trên Item.tres vì Resource đó được
	// dùng chung cho mọi ô cùng loại (sửa trực tiếp trên Resource sẽ ảnh hưởng tới
	// mọi stack khác đang trỏ tới cùng file .tres đó).
	// Dùng cho PrimaryItem: pin đèn pin, áp suất bình cứu hỏa còn lại...
	// null = chưa từng dùng -> coi như đầy (lấy theo PrimaryItem.MaxDurability khi cần).
	public float? CurrentDurability { get; set; } = null;

	// Dụng cụ (PrimaryItem) đang được bật/kích hoạt hay không.
	public bool IsActive { get; set; } = false;

	public bool IsEmpty => Item == null || Quantity <= 0;

	public void Clear()
	{
		Item = null;
		Quantity = 0;
		CurrentDurability = null;
		IsActive = false;
	}
}
