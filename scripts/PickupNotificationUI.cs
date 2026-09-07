using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;


public partial class PickupNotificationUI : CanvasLayer
{
	public static PickupNotificationUI Instance { get; private set; }

	[Export] public Label NotificationLabel;

	private readonly Queue<string> _queue = new();
	private bool _isShowing = false;

	public override void _Ready()
	{
		Instance = this;

		ProcessMode = ProcessModeEnum.Always;

		if (NotificationLabel != null)
		{
			NotificationLabel.Modulate = new Color(1, 1, 1, 0);
			NotificationLabel.Visible = false;
		}
	}

	public void ShowPickup(string itemName)
	{
		if (string.IsNullOrWhiteSpace(itemName)) return;

		_queue.Enqueue($"Nhận được {itemName}");

		if (!_isShowing)
			_ = ProcessQueue();
	}

	private async Task ProcessQueue()
	{
		_isShowing = true;

		while (_queue.Count > 0)
		{
			string text = _queue.Dequeue();
			await PlayOne(text);
		}

		_isShowing = false;
	}

	private async Task PlayOne(string text)
	{
		if (NotificationLabel == null) return;

		NotificationLabel.Text = text;
		NotificationLabel.Visible = true;

		var tween = CreateTween();
		tween.SetProcessMode(Tween.TweenProcessMode.Physics);
		tween.TweenProperty(NotificationLabel, "modulate:a", 1.0f, 0.2f);
		tween.TweenInterval(1.2);
		tween.TweenProperty(NotificationLabel, "modulate:a", 0.0f, 0.3f);

		await ToSignal(tween, Tween.SignalName.Finished);

		NotificationLabel.Visible = false;
	}
}
