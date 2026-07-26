using Godot;
using System.Collections.Generic;

public partial class JournalUI : CanvasLayer
{
	public static JournalUI Instance { get; private set; }

	[Export] public PackedScene EntryScene;
	[Export] public VBoxContainer EntryContainer;
	[Export] public Label ContentTitleLabel;
	[Export] public RichTextLabel ContentTextLabel;
	[Export] public RichTextLabel ContentThoughtLabel;

	private List<JournalEntry> _entryNodes = new();

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;

		Visible = false;

		if (GameManager.Instance != null)
		{
			GameManager.Instance.JournalChanged += RefreshUI;
		}

		BuildEntries();
		RefreshUI();
	}

	private void BuildEntries()
	{
		foreach (Node child in EntryContainer.GetChildren())
			child.QueueFree();
		_entryNodes.Clear();

		var journal = GameManager.Instance;
		if (journal == null) return;

		foreach (var item in journal.AllDocuments)
		{
			var entryNode = EntryScene.Instantiate<JournalEntry>();
			EntryContainer.AddChild(entryNode);
			entryNode.Setup(item);
			entryNode.EntrySelected += OnEntrySelected;
			_entryNodes.Add(entryNode);
		}
	}

	private void RefreshUI()
	{
		foreach (var entry in _entryNodes)
			entry.Refresh();
	}

	private void OnEntrySelected(Item item)
	{
		var journal = GameManager.Instance;
		if (item == null || journal == null || !journal.IsUnlocked(item)) return;

		ContentTitleLabel.Text = item.ItemName;
		ContentTextLabel.Text = item.Description;

		ContentThoughtLabel.Text = string.IsNullOrEmpty(item.Thought)
			? ""
			: $"[i]{item.Thought}[/i]";

		ContentThoughtLabel.Modulate = new Color(1, 1, 1, 0);

		var tween = CreateTween();
		tween.TweenInterval(0.6);
		tween.TweenProperty(ContentThoughtLabel, "modulate:a", 1.0f, 0.8f);
	}
}
