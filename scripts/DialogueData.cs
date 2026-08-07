using Godot;

[GlobalClass]
public partial class DialogueData : Resource
{
	[Export] public string DialogueName { get; set; } = "";
	[Export] public Godot.Collections.Array<DialogueLine> Lines { get; set; } = new();
	[Export] public int StartLineIndex { get; set; } = 0;

	[Export] public Item RequiredItem { get; set; } = null;
	[Export] public int RequiredQuantity { get; set; } = 1;

	public bool MeetsCondition()
	{
		if (RequiredItem == null) return true;
		return Inventory.Instance.HasItem(RequiredItem, RequiredQuantity);
	}
}
