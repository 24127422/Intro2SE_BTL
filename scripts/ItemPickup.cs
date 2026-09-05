using Godot;

public partial class ItemPickup : Area2D
{
	[Export] public Item ItemData { get; set; }

	public float? Durability { get; set; } = null;

	private Label _promptLabel;
	private bool _isPlayerInRange = false;
	private Inventory _inventory;

	public override void _Ready()
	{
		_inventory = GetNodeOrNull<Inventory>("/root/Inventory");

		_promptLabel = GetNode<Label>("Label");
		if (_promptLabel != null)
		{
			_promptLabel.Visible = false;
		}

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		if (ItemData == null)
		{
			GD.PrintErr($"[Cảnh báo] '{Name}' chưa được gán ItemData.");
		}

		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null && ItemData?.Icon != null)
		{
			sprite.Texture = ItemData.Icon;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			_isPlayerInRange = true;
			_promptLabel.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			_isPlayerInRange = false;
			if (_promptLabel != null) _promptLabel.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_isPlayerInRange && Input.IsActionJustPressed("interact"))
		{
			if (ItemData == null) return;

			if (ItemData.IsDocument)
			{
				if (DocumentJournal.Instance != null)
				{
					DocumentJournal.Instance.UnlockDocument(ItemData);
					QueueFree();
				}
				return;
			}

			if (_inventory == null)
			{
				GD.PrintErr("[LỖI] Autoload 'Inventory' chưa được thiết lập trong Project Settings!");
				return;
			}

			bool success = _inventory.AddItem(ItemData, 1, Durability);
			if (success)
			{
				ShowFirstThoughtIfAny(ItemData);
				QueueFree();
			}
		}
	}

	// Hiện đúng 1 lần "suy nghĩ" đầu tiên của nhân vật khi nhặt 1 LOẠI item lần đầu tiên
	// trong suốt phiên chơi (không phải mỗi lần nhặt thêm cùng loại đó).
	// Dùng chung DialogueUI có sẵn — không cần UI mới, chỉ 1 dòng thoại không tên người nói.
	private void ShowFirstThoughtIfAny(Item item)
	{
		if (item == null) return;
		if (item.ThoughtShown) return;
		if (string.IsNullOrWhiteSpace(item.Thought)) return;

		item.ThoughtShown = true;

		var line = new DialogueLine
		{
			SpeakerName = "",
			Text = item.Thought,
			NextLineIndex = -1,
		};

		var dyn = new DialogueData
		{
			Lines = new Godot.Collections.Array<DialogueLine> { line },
			StartLineIndex = 0,
		};

		DialogueUI.Instance.StartDialogue(dyn, null);
	}
}
