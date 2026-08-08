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

	public List<string> GetUnlockedDocumentPaths()
	{
		var paths = new List<string>();
		foreach (var item in _unlocked)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.ResourcePath))
				paths.Add(item.ResourcePath);
		}
		return paths;
	}

	public void RestoreUnlockedDocuments(IEnumerable<string> paths)
	{
		_unlocked.Clear();
		foreach (var path in paths)
		{
			if (string.IsNullOrWhiteSpace(path))
				continue;

			var item = ResourceLoader.Load<Item>(path);
			if (item != null)
				_unlocked.Add(item);
		}

		EmitSignal(SignalName.JournalChanged);
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
