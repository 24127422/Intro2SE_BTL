using Godot;
using System.Collections.Generic;

public partial class DocumentJournal : Node
{
	public static DocumentJournal Instance { get; private set; }

	[Export] public Godot.Collections.Array<Item> AllDocuments { get; set; } = new();

	private HashSet<Item> _unlocked = new();

	[Signal] public delegate void JournalChangedEventHandler();
	[Signal] public delegate void DocumentUnlockedEventHandler(Item item);

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
	}

	public bool IsUnlocked(Item item)
	{
		return item != null && _unlocked.Contains(item);
	}

	public void UnlockDocument(Item item)
	{
		if (item == null || !item.IsDocument) return;
		if (_unlocked.Contains(item)) return;

		_unlocked.Add(item);
		EmitSignal(SignalName.DocumentUnlocked, item);
		EmitSignal(SignalName.JournalChanged);
	}
}
