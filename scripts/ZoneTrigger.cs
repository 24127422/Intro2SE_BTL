using Godot;

public partial class ZoneTrigger : Area2D
{
	[Export] public string ZoneName;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		ZoneRegistry.Instance?.RegisterZone(this);
	}

	public override void _ExitTree() => ZoneRegistry.Instance?.UnregisterZone(this);

	private void OnBodyEntered(Node2D body)
	{
		if (body is movement)
			ZoneStatsTracker.Instance?.OnPlayerEnterZone(ZoneName);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is movement)
			ZoneStatsTracker.Instance?.OnPlayerExitZone(ZoneName);
	}

	public Vector2 GetWaypoint() => GlobalPosition;
}
