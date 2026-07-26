using Godot;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	// === TRẠNG THÁI GAME ===
	public enum GameState
	{
		MainMenu,
		Playing,
		Paused,
		Victory,
		Defeat
	}

	[ExportGroup("Game State")]
	[Export] public GameState CurrentState { get; private set; } = GameState.MainMenu;

	// === TÍN HIỆU (SIGNALS) ===
	[Signal] public delegate void GameStateChangedEventHandler(int newState);
	[Signal] public delegate void ObjectiveProgressUpdatedEventHandler(int current, int total);
	[Signal] public delegate void GameVictoryEventHandler();
	[Signal] public delegate void GameDefeatEventHandler(string reason);
	[Signal] public delegate void SettingsAppliedEventHandler();

	// === ĐIỀU KIỆN THẮNG / THUA ===
	[ExportGroup("Win / Loss Conditions")]
	[Export] public bool CheckWinByDocuments { get; set; } = true;
	[Export] public int RequiredDocumentCount { get; set; } = 5;
	[Export] public float TimeLimitSeconds { get; set; } = 0.0f; // 0 = Không giới hạn thời gian

	public int CurrentCollectedDocuments { get; private set; } = 0;
	public float ElapsedTime { get; private set; } = 0.0f;
	private bool _isTimerRunning = false;

	// === DỮ LIỆU SETTINGS ===
	private const string SettingsFilePath = "user://settings.cfg";

	public float MasterVolume { get; private set; } = 1.0f; // Từ 0.0 đến 1.0
	public float MusicVolume { get; private set; } = 0.8f;
	public float SFXVolume { get; private set; } = 0.8f;

	public DisplayServer.WindowMode CurrentWindowMode { get; private set; } = DisplayServer.WindowMode.Windowed;
	public Vector2I CurrentResolution { get; private set; } = new Vector2I(1280, 720);
	public bool VSyncEnabled { get; private set; } = true;

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;

		// Tải và áp dụng cài đặt ngay khi game khởi chạy
		LoadSettings();
		ApplySettings();

		// Kết nối với DocumentJournal nếu đã tồn tại
		if (DocumentJournal.Instance != null)
		{
			DocumentJournal.Instance.DocumentUnlocked += OnDocumentUnlockedFromJournal;
		}
	}

	#region Game State Management

	public void StartGame()
	{
		CurrentState = GameState.Playing;
		ElapsedTime = 0.0f;
		CurrentCollectedDocuments = 0;
		_isTimerRunning = true;

		GetTree().Paused = false;
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
		EmitSignal(SignalName.ObjectiveProgressUpdated, CurrentCollectedDocuments, RequiredDocumentCount);
	}

	public void TogglePause()
	{
		if (CurrentState == GameState.Playing)
		{
			CurrentState = GameState.Paused;
			GetTree().Paused = true;
			_isTimerRunning = false;
		}
		else if (CurrentState == GameState.Paused)
		{
			CurrentState = GameState.Playing;
			GetTree().Paused = false;
			_isTimerRunning = true;
		}

		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
	}

	public void TriggerVictory()
	{
		if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;

		CurrentState = GameState.Victory;
		_isTimerRunning = false;
		GetTree().Paused = true;

		GD.Print("[GameManager] VICTORY! Bạn đã hoàn thành trò chơi.");
		EmitSignal(SignalName.GameVictory);
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
	}

	public void TriggerDefeat(string reason = "Bạn đã thất bại!")
	{
		if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;

		CurrentState = GameState.Defeat;
		_isTimerRunning = false;
		GetTree().Paused = true;

		GD.Print($"[GameManager] GAME OVER! Lý do: {reason}");
		EmitSignal(SignalName.GameDefeat, reason);
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
	}

	#endregion

	#region Objective Tracking

	private void OnDocumentUnlockedFromJournal(Item item)
	{
		if (!CheckWinByDocuments || CurrentState != GameState.Playing) return;

		CurrentCollectedDocuments++;
		EmitSignal(SignalName.ObjectiveProgressUpdated, CurrentCollectedDocuments, RequiredDocumentCount);

		// Kiểm tra điều kiện thắng
		if (CurrentCollectedDocuments >= RequiredDocumentCount)
		{
			TriggerVictory();
		}
	}

	#endregion

	#region Settings System (Save/Load/Apply)

	public void SetVolume(string busName, float volumeLinear)
	{
		volumeLinear = Mathf.Clamp(volumeLinear, 0.0001f, 1.0f); // Tránh log(0)
		int busIndex = AudioServer.GetBusIndex(busName);

		if (busIndex != -1)
		{
			AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(volumeLinear));
		}

		switch (busName.ToLower())
		{
			case "master": MasterVolume = volumeLinear; break;
			case "music": MusicVolume = volumeLinear; break;
			case "sfx": SFXVolume = volumeLinear; break;
		}
	}

	public void SetDisplayMode(DisplayServer.WindowMode mode, Vector2I resolution, bool vsync)
	{
		CurrentWindowMode = mode;
		CurrentResolution = resolution;
		VSyncEnabled = vsync;

		DisplayServer.WindowSetMode(mode);
		if (mode == DisplayServer.WindowMode.Windowed)
		{
			DisplayServer.WindowSetSize(resolution);
		}

		DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
	}

	public void ApplySettings()
	{
		SetVolume("Master", MasterVolume);
		SetVolume("Music", MusicVolume);
		SetVolume("SFX", SFXVolume);

		SetDisplayMode(CurrentWindowMode, CurrentResolution, VSyncEnabled);

		EmitSignal(SignalName.SettingsApplied);
	}

	public void SaveSettings()
	{
		var config = new ConfigFile();

		// Audio
		config.SetValue("Audio", "MasterVolume", MasterVolume);
		config.SetValue("Audio", "MusicVolume", MusicVolume);
		config.SetValue("Audio", "SFXVolume", SFXVolume);

		// Video
		config.SetValue("Video", "WindowMode", (int)CurrentWindowMode);
		config.SetValue("Video", "ResolutionX", CurrentResolution.X);
		config.SetValue("Video", "ResolutionY", CurrentResolution.Y);
		config.SetValue("Video", "VSync", VSyncEnabled);

		config.Save(SettingsFilePath);
		GD.Print("[GameManager] Cài đặt đã được lưu vào user://settings.cfg");
	}

	public void LoadSettings()
	{
		var config = new ConfigFile();
		Error err = config.Load(SettingsFilePath);

		if (err != Error.Ok)
		{
			GD.Print("[GameManager] Không tìm thấy file settings.cfg, sử dụng thiết lập mặc định.");
			return;
		}

		// Audio
		MasterVolume = (float)config.GetValue("Audio", "MasterVolume", 1.0f);
		MusicVolume = (float)config.GetValue("Audio", "MusicVolume", 0.8f);
		SFXVolume = (float)config.GetValue("Audio", "SFXVolume", 0.8f);

		// Video
		CurrentWindowMode = (DisplayServer.WindowMode)(int)config.GetValue("Video", "WindowMode", (int)DisplayServer.WindowMode.Windowed);
		int resX = (int)config.GetValue("Video", "ResolutionX", 1280);
		int resY = (int)config.GetValue("Video", "ResolutionY", 720);
		CurrentResolution = new Vector2I(resX, resY);
		VSyncEnabled = (bool)config.GetValue("Video", "VSync", true);
	}

	#endregion
}