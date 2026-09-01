using Godot;
using System.Threading.Tasks;

public partial class Toilet : Area2D
{
	private bool _playerInRange = false;
	private bool _isPeeing = false;

	private movement _player;

	private Label _label;
	private AudioStreamPlayer2D _audio;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_audio = GetNode<AudioStreamPlayer2D>("AudioStreamPlayer2D");

		_label.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is movement player)
		{
			_player = player;
			_playerInRange = true;

			if (!_isPeeing)
				_label.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body != _player)
			return;

		_playerInRange = false;

		if (!_isPeeing)
		{
			_player = null;
			_label.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (!_playerInRange || _player == null || _isPeeing)
			return;

		if (Input.IsActionJustPressed("interact"))
		{
			_ = Pee();
		}
	}

	private async Task Pee()
	{
		if (_player == null || _isPeeing)
			return;

		_isPeeing = true;

		// Ẩn prompt
		_label.Visible = false;

		// Player quay mặt về phía toilet
		_player.FaceTowards(GlobalPosition);

		// Khóa input player
		if (GameManager.Instance != null)
		{
			GameManager.Instance.SetInteractionInputBlocked(true);
		}

		// Phát sound
		_audio.Play();

		// Chờ sound phát xong
		await ToSignal(
			_audio,
			AudioStreamPlayer2D.SignalName.Finished
		);

		// Mở lại input
		if (GameManager.Instance != null)
		{
			GameManager.Instance.SetInteractionInputBlocked(false);
		}

		_isPeeing = false;

		// Nếu player vẫn ở trong vùng toilet
		if (_playerInRange && _player != null)
		{
			_label.Visible = true;
		}
		else
		{
			_player = null;
		}
	}
}
