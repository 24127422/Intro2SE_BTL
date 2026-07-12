using Godot;

public partial class NPC : Area2D
{
	[Export] public Texture2D NpcIcon { get; set; }
	[Export] public Godot.Collections.Array<DialogueData> DialogueStages { get; set; } = new();
	private Label _promptLabel;
	private bool _isPlayerInRange = false;

	public override void _Ready()
	{
		if (NpcIcon != null)
			GetNode<Sprite2D>("Sprite2D").Texture = NpcIcon;
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
		if (_isPlayerInRange && !DialogueUI.Instance.IsTalking
			&& Input.IsActionJustPressed("interact"))
		{
			var dialogue = PickDialogue();
			if (dialogue != null)
			{
				_promptLabel.Visible = false;
				DialogueUI.Instance.StartDialogue(dialogue, this);
			}
		}
	}

	public void OnDialogueEnded()
	{
		if (_isPlayerInRange)
			_promptLabel.Visible = true;
	}

	private DialogueData PickDialogue()
	{
		for (int i = DialogueStages.Count - 1; i >= 0; i--)
		{
			if (DialogueStages[i] != null && DialogueStages[i].MeetsCondition())
				return DialogueStages[i];
		}
		return null;
	}
}
