// 1 ô trong túi đồ
public class InventorySlotData
{
	public Item Item { get; set; } = null;
	public int Quantity { get; set; } = 0;

	public bool IsEmpty => Item == null || Quantity <= 0;

	public void Clear()
	{
		Item = null;
		Quantity = 0;
	}
}
