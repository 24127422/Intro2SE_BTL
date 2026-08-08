using Godot;
using System.Collections.Generic;
using System.Text;

public partial class ComputerAssemblyStation : Area2D
{
	[Export] public Godot.Collections.Array<AssemblyPart> RequiredParts { get; set; } = new();

	[Export(PropertyHint.MultilineText)]
	public string IntroText { get; set; } = "";

	[Export] public DialogueData CompleteDialogue { get; set; }

	[Export] public Item RewardItem { get; set; }
	[Export] public int RewardQuantity { get; set; } = 1;

	private readonly HashSet<Item> _inserted = new();
	public bool IsAssembled { get; private set; } = false;

	private Label _promptLabel;
	private bool _isPlayerInRange = false;
	private bool _hasShownIntro = false;

	public override void _Ready()
	{
		_promptLabel = GetNode<Label>("Label");
		_promptLabel.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			_isPlayerInRange = true;
			if (!DialogueUI.Instance.IsTalking)
				_promptLabel.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			_isPlayerInRange = false;
			_promptLabel.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_isPlayerInRange && !DialogueUI.Instance.IsTalking && !_promptLabel.Visible)
			_promptLabel.Visible = true;

		if (!_isPlayerInRange || DialogueUI.Instance.IsTalking) return;
		if (!Input.IsActionJustPressed("interact")) return;

		_promptLabel.Visible = false;
		Interact();
	}

	private void Interact()
	{
		if (IsAssembled)
		{
			if (CompleteDialogue != null)
				DialogueUI.Instance.StartDialogue(CompleteDialogue, null);
			return;
		}

		// Bước 1: lắp mọi linh kiện hợp lệ mà người chơi đang mang theo lúc này
		var justInserted = new List<string>();
		foreach (var part in RequiredParts)
		{
			if (part?.RequiredItem == null) continue;
			if (_inserted.Contains(part.RequiredItem)) continue;

			if (Inventory.Instance.HasItem(part.RequiredItem, part.RequiredQuantity))
			{
				Inventory.Instance.RemoveItem(part.RequiredItem, part.RequiredQuantity);
				_inserted.Add(part.RequiredItem);
				justInserted.Add(part.RequiredItem.ItemName);
			}
		}

		// Bước 2: kiểm tra đã đủ hết chưa
		bool complete = true;
		var stillMissing = new List<string>();
		foreach (var part in RequiredParts)
		{
			if (part?.RequiredItem == null) continue;
			if (!_inserted.Contains(part.RequiredItem))
			{
				complete = false;
				stillMissing.Add($"{part.RequiredItem.ItemName} x{part.RequiredQuantity}");
			}
		}

		if (complete)
		{
			CompleteAssembly();
			return;
		}

		ShowProgressDialogue(justInserted, stillMissing);
	}

	private void CompleteAssembly()
	{
		IsAssembled = true;

		if (RewardItem != null)
		{
			if (RewardItem.IsDocument)
				DocumentJournal.Instance?.UnlockDocument(RewardItem);
			else
				Inventory.Instance.AddItem(RewardItem, RewardQuantity);
		}

		if (CompleteDialogue != null)
			DialogueUI.Instance.StartDialogue(CompleteDialogue, null);
	}

	private void ShowProgressDialogue(List<string> justInserted, List<string> stillMissing)
	{
		var sb = new StringBuilder();

		if (!_hasShownIntro && !string.IsNullOrEmpty(IntroText))
		{
			sb.AppendLine(IntroText);
			sb.AppendLine();
			_hasShownIntro = true;
		}

		if (justInserted.Count > 0)
		{
			sb.AppendLine("Đã lắp: " + string.Join(", ", justInserted));
			sb.AppendLine();
		}

		sb.AppendLine("Còn thiếu:");
		foreach (var missing in stillMissing)
			sb.AppendLine("- " + missing);

		var line = new DialogueLine
		{
			SpeakerName = "Máy tính cũ",
			Text = sb.ToString().TrimEnd(),
			NextLineIndex = -1,
		};

		var dynamicDialogue = new DialogueData
		{
			Lines = new Godot.Collections.Array<DialogueLine> { line },
			StartLineIndex = 0,
		};

		DialogueUI.Instance.StartDialogue(dynamicDialogue, null);
	}
}
