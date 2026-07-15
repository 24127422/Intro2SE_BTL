using Godot;

// Một lựa chọn trả lời trong hội thoại (hiện dưới dạng nút bấm)
[GlobalClass]
public partial class DialogueChoice : Resource
{
	
	[Export] public string ChoiceText { get; set; } = "";

	// Chỉ số dòng thoại tiếp theo trong DialogueData.Lines. -1 = kết thúc hội thoại
	[Export] public int NextLineIndex { get; set; } = -1;

	// ĐIỀU KIỆN: chỉ hiện lựa chọn này nếu người chơi có đủ item.
	// Để trống (null) nếu lựa chọn này luôn hiện, không cần điều kiện.
	[Export] public Item RequiredItem { get; set; } = null;
	[Export] public int RequiredQuantity { get; set; } = 1;

	// Có tiêu hao (xóa khỏi túi) item điều kiện khi chọn lựa chọn này không
	// Vd: NPC nhờ đưa 1 cái chìa khóa -> chọn xong thì mất chìa khóa
	[Export] public bool ConsumeRequiredItem { get; set; } = false;

	public bool IsAvailable()
	{
		if (RequiredItem == null) return true;
		return Inventory.Instance.HasItem(RequiredItem, RequiredQuantity);
	}
}
