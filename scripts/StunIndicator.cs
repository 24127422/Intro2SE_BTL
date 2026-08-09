using Godot;

public partial class StunIndicator : CanvasLayer
{
	private ColorRect _fill;
	private Label _label;

	private Node2D _target;

	private float _duration;
	private float _timeLeft;

	public override void _Ready()
	{
		_fill = GetNode<ColorRect>("ColorRect");
		_label = GetNode<Label>("Label");

		_label.Text = "STUNNING";

		SetProcess(false);
	}

	public void Start(Node2D target, float duration)
	{
		_target = target;
		_duration = duration;
		_timeLeft = duration;

		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (_target == null || !GodotObject.IsInstanceValid(_target))
		{
			QueueFree();
			return;
		}

		Vector2 screenPos =
			_target.GetGlobalTransformWithCanvas().Origin;

		// Label
		_label.Position = screenPos + new Vector2(-32, -80);

		// Thanh stun
		_fill.Position = screenPos + new Vector2(-32, -64);

		_timeLeft -= (float)delta;

		float ratio = Mathf.Clamp(
			_timeLeft / _duration,
			0f,
			1f
		);

		_fill.Size = new Vector2(
			64f * ratio,
			_fill.Size.Y
		);

		if (_timeLeft <= 0f)
		{
			QueueFree();
		}
	}
}
