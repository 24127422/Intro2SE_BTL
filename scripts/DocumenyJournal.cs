using Godot;
using static DocumentJournal.SignalName;
using System.Collections.Generic;
using System.Linq;

public partial class DocumentJournal : Node
{
	public static DocumentJournal Instance { get; private set; }

	[Export] public Godot.Collections.Array<Item> AllDocuments { get; set; } = new();

	private readonly HashSet<Item> _unlocked = new();
	private readonly HashSet<Item> _readDocuments = new();

	[Signal] public delegate void JournalChangedEventHandler();
	[Signal] public delegate void DocumentUnlockedEventHandler(Item item);
	[Signal] public delegate void DocumentReadEventHandler(Item item);

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
	}

	#region Unlock & Read Logic

	public bool IsUnlocked(Item item) => item != null && _unlocked.Contains(item);
	public bool IsRead(Item item) => item != null && _readDocuments.Contains(item);

	public void UnlockDocument(Item item)
	{
		if (item == null || !item.IsDocument) return;
		if (_unlocked.Add(item))
		{
			EmitSignal(SignalName.DocumentUnlocked, item);
			EmitSignal(SignalName.JournalChanged);
		}
	}

	public void UnlockDocumentByName(string itemName)
	{
		var doc = AllDocuments.FirstOrDefault(d => d.ItemName == itemName);
		if (doc != null) UnlockDocument(doc);
	}

	public void MarkAsRead(Item item)
	{
		if (item == null || !_unlocked.Contains(item)) return;
		if (_readDocuments.Add(item))
		{
			EmitSignal(SignalName.DocumentRead, item);
			EmitSignal(SignalName.JournalChanged);
		}
	}

	#endregion

	#region Queries & Filters

	public List<Item> GetUnlockedDocuments(string searchQuery = "")
	{
		var list = _unlocked.ToList();
		if (string.IsNullOrWhiteSpace(searchQuery))
			return list;

		string q = searchQuery.ToLower();
		return list.Where(d => d.ItemName.ToLower().Contains(q) || 
                               d.Author.ToLower().Contains(q) ||
                               d.Description.ToLower().Contains(q)).ToList();
	}

	public int GetUnreadCount() => _unlocked.Count - _readDocuments.Count;

	#endregion

	#region Save / Load System

	public Godot.Collections.Array<string> GetUnlockedSaveData()
	{
		var arr = new Godot.Collections.Array<string>();
		foreach (var doc in _unlocked) arr.Add(doc.ItemName);
		return arr;
	}

	public Godot.Collections.Array<string> GetReadSaveData()
	{
		var arr = new Godot.Collections.Array<string>();
		foreach (var doc in _readDocuments) arr.Add(doc.ItemName);
		return arr;
	}

	public void LoadSaveData(Godot.Collections.Array<string> unlockedNames, Godot.Collections.Array<string> readNames)
	{
		_unlocked.Clear();
		_readDocuments.Clear();

		foreach (var name in unlockedNames)
		{
			var doc = AllDocuments.FirstOrDefault(d => d.ItemName == name);
			if (doc != null) _unlocked.Add(doc);
		}

		foreach (var name in readNames)
		{
			var doc = AllDocuments.FirstOrDefault(d => d.ItemName == name);
			if (doc != null) _readDocuments.Add(doc);
		}

		EmitSignal(SignalName.JournalChanged);
	}

	#endregion
}