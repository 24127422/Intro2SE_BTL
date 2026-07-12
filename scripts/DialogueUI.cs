using Godot;

public partial class DialogueUI : CanvasLayer
{
	public static DialogueUI Instance { get; private set; }

	[Export] public Panel Panel;
	[Export] public Label NameLabel;
	[Export] public Label TextLabel;
	[Export] public VBoxContainer ChoiceContainer;

	[Export] public PackedScene ChoiceButtonScene;

	[Export] public Vector2 ChoiceButtonMinSize = new Vector2(200, 36);

	[Export] public float TypeSpeed = 0.02f;

	public bool IsTalking { get; private set; } = false;

	private DialogueData _currentDialogue;
	private NPC _currentNpc;
	private int _currentLineIndex;
	private string _fullText = "";
	private double _typeTimer = 0;
	private int _typedChars = 0;
	private bool _isTyping = false;

	public override void _Ready()
	{
		Instance = this;
		Panel.Visible = false;
	}

	public void StartDialogue(DialogueData dialogue, NPC npc)
	{
		_currentDialogue = dialogue;
		_currentNpc = npc;
		IsTalking = true;
		Panel.Visible = true;
		ShowLine(dialogue.StartLineIndex);
	}

	private void ShowLine(int index)
	{
		if (_currentDialogue == null || index < 0 || index >= _currentDialogue.Lines.Count)
		{
			EndDialogue();
			return;
		}

		_currentLineIndex = index;
		var line = _currentDialogue.Lines[index];

		NameLabel.Text = line.SpeakerName;
		_fullText = line.Text;
		_typedChars = 0;
		_typeTimer = 0;
		_isTyping = TypeSpeed > 0f && _fullText.Length > 0;
		TextLabel.Text = _isTyping ? "" : _fullText;

		// item khi đạt condition
		if (line.RewardItem != null)
		{
			Inventory.Instance.AddItem(line.RewardItem, line.RewardQuantity);
			// Xóa tham chiếu để không thưởng lại nếu người chơi xem lại dòng này lần nữa
			// (chỉ có tác dụng trong 1 lần chạy game, xem ghi chú bên dưới)
			line.RewardItem = null;
		}

		ClearChoices();
		if (!_isTyping)
			ShowChoicesIfAny(line);
	}

	public override void _Process(double delta)
	{
		if (!IsTalking) return;
		if (!_isTyping) return;

		// Tự động gõ chữ từng ký tự
		_typeTimer += delta;
		int targetChars = Mathf.FloorToInt((float)(_typeTimer / TypeSpeed));
		targetChars = Mathf.Min(targetChars, _fullText.Length);
		if (targetChars != _typedChars)
		{
			_typedChars = targetChars;
			TextLabel.Text = _fullText.Substring(0, _typedChars);
		}
		if (_typedChars >= _fullText.Length)
		{
			// Gõ xong -> DỪNG lại chờ người chơi bấm chuột trái để tiếp tục
			_isTyping = false;
			ShowChoicesIfAny(_currentDialogue.Lines[_currentLineIndex]);
		}
	}

	// Dùng _UnhandledInput (không phải _Process) để click vào nút Choice
	// không bị tính luôn thành 1 lần "next dòng" (Button tự tiêu thụ input trước)
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsTalking) return;
		if (@event is not InputEventMouseButton mb) return;
		if (!mb.Pressed || mb.ButtonIndex != MouseButton.Left) return;

		var line = _currentDialogue.Lines[_currentLineIndex];
		if (_isTyping)
		{
			// Click khi đang gõ chữ -> hiện trọn câu ngay lập tức (không tính là next)
			_isTyping = false;
			_typedChars = _fullText.Length;
			TextLabel.Text = _fullText;
			ShowChoicesIfAny(line);
		}
		else if (line.Choices.Count == 0)
		{
			// Chữ đã hiện xong, không có lựa chọn -> click để sang dòng kế tiếp
			ShowLine(line.NextLineIndex);
		}
		// Nếu có lựa chọn: người chơi phải bấm nút, click nền không dùng để next nữa

		GetViewport().SetInputAsHandled();
	}

	private void ShowChoicesIfAny(DialogueLine line)
	{
		ClearChoices();
		if (line.Choices.Count == 0) return;

		foreach (var choice in line.Choices)
		{
			if (!choice.IsAvailable()) continue; // ẩn lựa chọn nếu chưa đủ điều kiện

			Button button = ChoiceButtonScene != null
				? ChoiceButtonScene.Instantiate<Button>()
				: new Button();

			button.Text = choice.ChoiceText;
			button.CustomMinimumSize = ChoiceButtonMinSize;
			button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
			button.Pressed += () => OnChoiceSelected(choice);
			ChoiceContainer.AddChild(button);
		}
	}

	private void OnChoiceSelected(DialogueChoice choice)
	{
		if (choice.RequiredItem != null && choice.ConsumeRequiredItem)
			Inventory.Instance.RemoveItem(choice.RequiredItem, choice.RequiredQuantity);

		ShowLine(choice.NextLineIndex);
	}

	private void ClearChoices()
	{
		foreach (Node child in ChoiceContainer.GetChildren())
			child.QueueFree();
	}

	private void EndDialogue()
	{
		IsTalking = false;
		Panel.Visible = false;
		_currentNpc?.OnDialogueEnded();
		_currentDialogue = null;
		_currentNpc = null;
	}
}
