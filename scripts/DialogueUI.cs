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
        
        // ĐĂNG KÝ SỰ KIỆN: Lắng nghe click chuột trực tiếp trên Panel để tránh bị nuốt mất sự kiện click
        Panel.GuiInput += OnPanelGuiInput;
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
        // Nếu chỉ số dòng tiếp theo là -1 (kết thúc) hoặc vượt quá số lượng dòng thoại
        if (_currentDialogue == null || index == -1 || index >= _currentDialogue.Lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = _currentDialogue.Lines[index];
        if (line == null)
        {
            EndDialogue();
            return;
        }

        _currentLineIndex = index;
        
        NameLabel.Text = line.SpeakerName;
        _fullText = line.Text;
        _typedChars = 0;
        _typeTimer = 0;
        _isTyping = TypeSpeed > 0f && _fullText.Length > 0;
        TextLabel.Text = _isTyping ? "" : _fullText;

        // Thưởng vật phẩm nếu có
        if (line.RewardItem != null)
        {
            Inventory.Instance.AddItem(line.RewardItem, line.RewardQuantity);
            // Xóa tham chiếu để không thưởng lại nếu người chơi xem lại dòng này lần nữa
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

    // Nhận sự kiện click chuột khi người chơi click TRỰC TIẾP lên khung hội thoại (Panel)
    private void OnPanelGuiInput(InputEvent @event)
    {
        HandleDialogueInput(@event);
    }

    // Nhận sự kiện click chuột khi người chơi click RA NGOÀI khung hội thoại
    public override void _UnhandledInput(InputEvent @event)
    {
        HandleDialogueInput(@event);
    }

    // Hàm xử lý logic click chuột chung để chuyển dòng hoặc tắt hội thoại
    private void HandleDialogueInput(InputEvent @event)
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
        // Kiểm tra an toàn tránh lỗi Null khi dòng thoại không có thuộc tính Choices
        else if (line.Choices == null || line.Choices.Count == 0)
        {
            // Chữ đã hiện xong, không có lựa chọn -> click để sang dòng kế tiếp hoặc kết thúc
            ShowLine(line.NextLineIndex);
        }

        GetViewport().SetInputAsHandled();
    }

    private void ShowChoicesIfAny(DialogueLine line)
    {
        ClearChoices();
        if (line.Choices == null || line.Choices.Count == 0) return;

        foreach (var choice in line.Choices)
        {
            if (choice == null) continue;
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
        if (choice == null) return;

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
        
        ClearChoices(); // DỌN SẠCH các nút lựa chọn cũ tránh rác và lỗi hiển thị lần sau
        
        _currentNpc?.OnDialogueEnded();
        _currentDialogue = null;
        _currentNpc = null;
    }
}