using Godot;
using System.Collections.Generic;
using System.Text;

public partial class ComputerAssemblyStation : Area2D
{
	// === 7 bộ phận "thân xác" bắt buộc: Case, Mainboard, PSU, CPU, Tản nhiệt, RAM, GPU ===
	[Export] public Godot.Collections.Array<AssemblyPart> RequiredParts { get; set; } = new();

	[Export(PropertyHint.MultilineText)]
	public string IntroText { get; set; } = "";

	// === Khe ROM: 1 ROM thật + N ROM giả. Người chơi chỉ có thể lắp ROM nào đang có
	// trong túi đồ — ROM nào lắp trước sẽ bị tiêu hao ngay (không rút ra lắp lại được),
	// nên chỉ nên để người chơi tìm thấy 1 trong 3 ROM ở mỗi lượt chơi để giữ độ căng thẳng. ===
	[ExportGroup("Khe ROM")]
	[Export] public Godot.Collections.Array<Item> ValidRoms { get; set; } = new();
	[Export] public Item CorrectRom { get; set; }
	[Export] public DialogueData GoodEndingDialogue { get; set; }
	[Export] public DialogueData BadEndingDialogue { get; set; }

	// === Màn hình đổi hình theo kết quả — chỉ cần gán 2 texture, không cần logic phức tạp ===
	[ExportGroup("Màn hình")]
	[Export] public Texture2D ScreenGoodTexture { get; set; }
	[Export] public Texture2D ScreenBadTexture { get; set; }

	[Export] public Item RewardItem { get; set; }
	[Export] public int RewardQuantity { get; set; } = 1;

	private readonly HashSet<Item> _inserted = new();
	public bool BodyComplete { get; private set; } = false;
	public bool IsAssembled { get; private set; } = false;
	private bool _romWasCorrect = false;

	private Label _promptLabel;
	private Sprite2D _screenSprite;
	private bool _isPlayerInRange = false;
	private bool _hasShownIntro = false;

	public override void _Ready()
	{
		_promptLabel = GetNode<Label>("Label");
		_promptLabel.Visible = false;
		_screenSprite = GetNodeOrNull<Sprite2D>("Sprite2D");

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
		bool otherUiOpen = DialogueUI.Instance.IsTalking || (RomSlotUI.Instance?.IsOpen ?? false);

		if (_isPlayerInRange && !otherUiOpen && !_promptLabel.Visible)
			_promptLabel.Visible = true;

		if (!_isPlayerInRange || otherUiOpen) return;
		if (!Input.IsActionJustPressed("interact")) return;

		_promptLabel.Visible = false;
		Interact();
	}

	private void Interact()
	{
		if (IsAssembled)
		{
			var dlg = _romWasCorrect ? GoodEndingDialogue : BadEndingDialogue;
			if (dlg != null)
				DialogueUI.Instance.StartDialogue(dlg, null);
			return;
		}

		if (!BodyComplete)
		{
			TryInsertBodyParts();
			return;
		}

		// Không tự quét túi đồ nữa — mở panel để người chơi TỰ kéo đúng ROM họ chọn vào khe.
		RomSlotUI.Instance?.Open(this);
	}

	// ---------------- Lắp 7 bộ phận thân xác ----------------

	private void TryInsertBodyParts()
	{
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
			BodyComplete = true;
			ShowBodyCompleteDialogue();
			return;
		}

		ShowProgressDialogue(justInserted, stillMissing);
	}

	// ---------------- Lắp ROM (thật hoặc giả) — gọi từ RomSlotUI khi người chơi thả ROM vào khe ----------------

	private void ShowBodyCompleteDialogue()
	{
		var line = new DialogueLine
		{
			SpeakerName = "Máy tính cũ",
			Text = "Mọi bộ phận đã lắp đủ. Chỉ còn thiếu duy nhất một ROM để khởi động.\n\nNhấn E lần nữa để chọn ROM.",
			NextLineIndex = -1,
		};

		var dyn = new DialogueData
		{
			Lines = new Godot.Collections.Array<DialogueLine> { line },
			StartLineIndex = 0,
		};

		DialogueUI.Instance.StartDialogue(dyn, null);
	}

	// Trả về false nếu chưa sẵn sàng nhận ROM hoặc item thả vào không nằm trong ValidRoms.
	// KHÔNG tự trừ item khỏi túi đồ — việc đó do RomSlotUI.TryHandleDrop() xử lý sau khi
	// hàm này trả về true, để tránh trừ nhầm khi item không hợp lệ.
	public bool TryInsertSpecificRom(Item rom)
	{
		if (!BodyComplete || IsAssembled) return false;
		if (rom == null) return false;

		bool isValid = false;
		foreach (var validRom in ValidRoms)
		{
			if (validRom == rom) { isValid = true; break; }
		}
		if (!isValid) return false;

		_romWasCorrect = (rom == CorrectRom);
		IsAssembled = true;

		if (_screenSprite != null)
		{
			var tex = _romWasCorrect ? ScreenGoodTexture : ScreenBadTexture;
			if (tex != null)
				_screenSprite.Texture = tex;
		}

		if (_romWasCorrect && RewardItem != null)
		{
			if (RewardItem.IsDocument)
				DocumentJournal.Instance?.UnlockDocument(RewardItem);
			else
				Inventory.Instance.AddItem(RewardItem, RewardQuantity);
		}

		var dlg = _romWasCorrect ? GoodEndingDialogue : BadEndingDialogue;
		if (dlg != null)
			DialogueUI.Instance.StartDialogue(dlg, null);

		return true;
	}

	// ---------------- Dialogue tiến trình lắp thân xác ----------------

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
