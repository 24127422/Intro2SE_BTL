using Godot;

// Một lựa chọn trả lời trong hội thoại (hiện dưới dạng nút bấm)
[GlobalClass]
public partial class DialogueChoice : Resource
{
	
	[Export] public string ChoiceText { get; set; } = "";

	[Export] public int NextLineIndex { get; set; } = -1;

	[Export] public Item RequiredItem { get; set; } = null;
	[Export] public int RequiredQuantity { get; set; } = 1;


	[Export] public bool ConsumeRequiredItem { get; set; } = false;

	public bool IsAvailable()
	{
		if (RequiredItem == null) return true;
		return Inventory.Instance.HasItem(RequiredItem, RequiredQuantity);
	}
}
