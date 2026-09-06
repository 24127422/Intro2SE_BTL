using Godot;
using System.Collections.Generic;

public partial class ZoneRegistry : Node
{
	public static ZoneRegistry Instance { get; private set; }
	private readonly Dictionary<string, ZoneTrigger> _zones = new();

	public override void _EnterTree() => Instance = this;

	public void RegisterZone(ZoneTrigger zone)
	{
		if (!string.IsNullOrEmpty(zone.ZoneName)) _zones[zone.ZoneName] = zone;
	}

	public void UnregisterZone(ZoneTrigger zone)
	{
		if (_zones.TryGetValue(zone.ZoneName, out var z) && z == zone)
			_zones.Remove(zone.ZoneName);
	}

	public Vector2? GetWaypoint(string zoneName)
		=> _zones.TryGetValue(zoneName, out var z) ? z.GetWaypoint() : null;

	// Enemy chưa biết mình đang ở zone nào -> lấy zone có waypoint gần vị trí nó nhất
	public string GetNearestZone(Vector2 position)
	{
		string best = null;
		float bestDist = float.MaxValue;
		foreach (var kv in _zones)
		{
			float d = kv.Value.GetWaypoint().DistanceSquaredTo(position);
			if (d < bestDist) { bestDist = d; best = kv.Key; }
		}
		return best;
	}
}
