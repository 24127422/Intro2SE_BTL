using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class ZoneStatsTracker : Node
{
	public static ZoneStatsTracker Instance { get; private set; }
	private const string SavePath = "user://zone_stats.json";
	private Dictionary<string, float> _secondsInZone = new();
	private string _currentPlayerZone;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree() => Save();

	public void OnPlayerEnterZone(string zoneName) => _currentPlayerZone = zoneName;

	public void OnPlayerExitZone(string zoneName)
	{
		if (_currentPlayerZone == zoneName)
			_currentPlayerZone = null;
	}

	public override void _Process(double delta)
	{
		if (_currentPlayerZone == null) return;
		_secondsInZone.TryAdd(_currentPlayerZone, 0f);
		_secondsInZone[_currentPlayerZone] += (float)delta;
	}

	public Dictionary<string, float> GetStats() => new(_secondsInZone);

	public void Save()
	{
		try
		{
			using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
			file?.StoreString(JsonSerializer.Serialize(_secondsInZone));
		}
		catch (System.Exception ex) { GD.PrintErr($"[ZoneStatsTracker] Lỗi lưu: {ex.Message}"); }
	}
}
