using Godot;
using System.Collections.Generic;

public partial class Door : Sprite2D
{
	[Export] public Node2D TargetPoint { get; set; }

	[Export] public string TransitionName { get; set; }

	[Export] public float EnemyTeleportDelay = 3f;

	private bool _isPlayerInRange = false;
	private CharacterBody2D _player;
	private Label _promptLabel;

	private readonly Dictionary<Enemy, float> _enemyTimers = new();

	public override void _Ready()
	{
		_promptLabel = GetNodeOrNull<Label>("Label");
		if (_promptLabel != null)
			_promptLabel.Visible = false;

		Area2D area = GetNode<Area2D>("Area2D");
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;

		if (!string.IsNullOrEmpty(TransitionName))
			TransitionRegistry.Instance?.RegisterTransition(TransitionName, this);
	}

	public override void _ExitTree()
	{
		if (!string.IsNullOrEmpty(TransitionName))
			TransitionRegistry.Instance?.UnregisterTransition(TransitionName, this);
	}

	public Vector2 GetWaypoint() => GlobalPosition;

	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.Name == "Player")
		{
			_player = player;
			_isPlayerInRange = true;
			if (_promptLabel != null) _promptLabel.Visible = true;
		}
		else if (body is Enemy enemy)
		{
			_enemyTimers[enemy] = 0f;

			if (!string.IsNullOrEmpty(TransitionName))
				enemy.NotifyEnteredDoorZone(TransitionName);
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
			_isPlayerInRange = false;
			if (_promptLabel != null) _promptLabel.Visible = false;
		}
		else if (body is Enemy enemy)
		{
			_enemyTimers.Remove(enemy);

			if (!string.IsNullOrEmpty(TransitionName) && GodotObject.IsInstanceValid(enemy))
				enemy.NotifyExitedDoorZone(TransitionName);
		}
	}

	public override void _Process(double delta)
	{
		// Player: bấm phím để dịch chuyển
		if (_isPlayerInRange && _player != null && Input.IsActionJustPressed("interact"))
		{
			if (TargetPoint != null)
			{
				_player.GlobalPosition = TargetPoint.GlobalPosition;
				_player = null;
				_isPlayerInRange = false;
				if (_promptLabel != null) _promptLabel.Visible = false;
			}
		}

		// Enemy: đứng đủ EnemyTeleportDelay giây thì tự động dịch chuyển
		if (_enemyTimers.Count == 0) return;

		var keys = new List<Enemy>(_enemyTimers.Keys);
		foreach (var enemy in keys)
		{
			if (!GodotObject.IsInstanceValid(enemy))
			{
				_enemyTimers.Remove(enemy);
				continue;
			}

			// Chỉ đếm giờ khi enemy thực sự đang chủ động chờ qua cửa này (đang patrol theo route).
			// Nếu enemy chuyển sang Chase/Attack/Stunned giữa chừng thì reset về 0, không tự ý dịch chuyển.
			if (!enemy.IsAwaitingDoorTeleport)
			{
				_enemyTimers[enemy] = 0f;
				continue;
			}

			_enemyTimers[enemy] += (float)delta;
			if (_enemyTimers[enemy] >= EnemyTeleportDelay)
			{
				_enemyTimers.Remove(enemy);

				if (TargetPoint != null)
				{
					enemy.GlobalPosition = TargetPoint.GlobalPosition;
					enemy.OnDoorTeleported();
				}
			}
		}
	}
}
