using Godot;

public partial class JournalEntry : Button
{
	public Item DocumentItem { get; private set; }

	[Signal] public delegate void EntrySelectedEventHandler(Item item);

	public override void _Pressed()
	{
		EmitSignal(SignalName.EntrySelected, DocumentItem);
	}

	public void Setup(Item item)
	{
		DocumentItem = item;
		Refresh();
	}

	public void Refresh()
	{ 	
		bool unlocked = DocumentJournal.Instance.IsUnlocked(DocumentItem);

		Text = unlocked ? DocumentItem.ItemName : "???";
		Disabled = !unlocked; // mục chưa mở khóa thì không bấm được
		Modulate = unlocked ? new Color(1, 1, 1, 1) : new Color(0.4f, 0.4f, 0.4f, 1f); // tối đi khi khóa
	}
}
