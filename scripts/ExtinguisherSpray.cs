using Godot;

public partial class ExtinguisherSpray : Node2D
{
	private Node2D _origin;
	private AnimatedSprite2D _sprite;

	public void AttachTo(Node2D origin)
	{
		_origin = origin;
	}

	public override async void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Đăng ký va chạm
		var area = GetNode<Area2D>("Area2D");
		area.BodyEntered += OnBodyEntered;

		// Scale cố định
		Scale = Vector2.One;

		// Play animation
		_sprite.Play("spray");

		// Chờ animation kết thúc rồi tự hủy
		await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);

		QueueFree();
	}

	public override void _Process(double delta)
	{
		if (_origin == null || !GodotObject.IsInstanceValid(_origin))
			return;

		// Bám theo tay hiện tại
		GlobalPosition = _origin.GlobalPosition;

		// Hướng theo chuột
		Vector2 mousePos = GetViewport()
			.GetCamera2D()
			.GetGlobalMousePosition();

		Vector2 dir = (mousePos - GlobalPosition).Normalized();

		GlobalRotation = dir.Angle();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Enemy enemy)
		{
			enemy.ApplyExtinguisherHit();
		}
	}
}
