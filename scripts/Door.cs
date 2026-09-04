using Godot;

public partial class Door : Sprite2D
{
	[Export]
	public Node2D TargetPoint { get; set; }

	private bool _isPlayerInRange = false;
	private CharacterBody2D _player;
	private Label _promptLabel;

	public override void _Ready()
	{
		_promptLabel = GetNode<Label>("Label");
		_promptLabel.Visible = false;

		Area2D area = GetNode<Area2D>("Area2D");

		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.Name == "Player")
		{
			_player = player;
			_isPlayerInRange = true;

			_promptLabel.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
			_isPlayerInRange = false;

			_promptLabel.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (!_isPlayerInRange || _player == null)
			return;

		if (Input.IsActionJustPressed("interact"))
		{
			if (TargetPoint == null)
				return;

			_player.GlobalPosition = TargetPoint.GlobalPosition;

			_player = null;
			_isPlayerInRange = false;
			_promptLabel.Visible = false;
		}
	}
}
