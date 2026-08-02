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
		Dialogue,
		GameOver
	}

	[ExportGroup("Game State")]
	[Export] public GameState CurrentState { get; private set; } = GameState.MainMenu;

	[ExportGroup("Overlay Flags")]
	public bool IsJournalOpen {get; private set; } = false;
	public bool IsInventoryOpen {get; private set; } = false;
	public bool IsPlayerInputBlocked => CurrentState != GameState.Playing || IsJournalOpen || IsInventoryOpen;

	private Label _debugLabel;
	// === TÍN HIỆU (SIGNALS) ===
	[Signal] public delegate void GameStateChangedEventHandler(int newState);
	[Signal] public delegate void SettingsAppliedEventHandler();

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

		LoadSettings();
		ApplySettings();

		StartGame();

		// dưới là hàm dùng để debug, tắt comment để vào mode debug.
		/* if (OS.IsDebugBuild())
		{
			_debugLabel = new Label();
			_debugLabel.Position = new Vector2(10, 10);
			GetTree().Root.CallDeferred("add_child", _debugLabel);
		}
		*/ 
	}
// dưới là hàm debug
/*  
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!OS.IsDebugBuild()) return;
		if (@event is not InputEventKey key || !key.Pressed) return;
		if (!key.CtrlPressed) return; // require Ctrl held, so single letters can't accidentally fire during normal typing/gameplay

		switch (key.Keycode)
		{
			case Key.F: // Ctrl+F
				Mock_SetState(GameState.Dialogue);
				GD.Print($"[DEBUG] State -> Dialogue. Blocked={IsPlayerInputBlocked}");
				break;

			case Key.G: // Ctrl+G
				Mock_SetState(GameState.Playing);
				GD.Print($"[DEBUG] State -> Playing. Blocked={IsPlayerInputBlocked}");
				break;

			case Key.H: // Ctrl+H
				Mock_OpenJournal();
				GD.Print($"[DEBUG] Journal open attempt. IsJournalOpen={IsJournalOpen}");
				break;

			case Key.J: // Ctrl+H
				Mock_CloseJournal();
				GD.Print($"[DEBUG] Journal closed. IsJournalOpen={IsJournalOpen}");
				break;

			case Key.K: // Ctrl+K
				Mock_SetState(GameState.GameOver);
				GD.Print($"[DEBUG] State -> GameOver. Blocked={IsPlayerInputBlocked}");
				break;
		}
	}

	public override void _Process(double delta)
	{
		if (_debugLabel != null)
		{
			_debugLabel.Text = $"State: {CurrentState} | Journal: {IsJournalOpen} | Inventory: {IsInventoryOpen} | Blocked: {IsPlayerInputBlocked}";
		}
	}
*/

	#region Game State Management

	public void StartGame()
	{
		SetState(GameState.Playing);
	}

	public void TogglePause()
	{
		if (CurrentState == GameState.Playing)
		{
			SetState(GameState.Paused);
		}
		else if (CurrentState == GameState.Paused)
		{
			SetState(GameState.Playing);
		}
	}

	public void StartDialogue()
	{
		if (CurrentState == GameState.Playing)
		{
			SetState(GameState.Dialogue);
		}
	}

	public void EndDialogue()
	{
		if (CurrentState == GameState.Dialogue)
		{
			SetState(GameState.Playing);
		}
	}

	public void TriggerGameOver()
	{
		SetState(GameState.GameOver);
	}

	public void SetJournalOpen(bool open)
	{
		if (open && CurrentState != GameState.Playing) return;
		IsJournalOpen = open;
	}

	public void SetInventoryOpen(bool open)
	{
		if (open && CurrentState != GameState.Playing) return;
		IsInventoryOpen = open;
	}
	public void SetState(GameState newState)
	{
		CurrentState = newState;
		GetTree().Paused = (newState == GameState.Paused);
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
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

	#region Testing/ Mocking

	public void Mock_SetState(GameState state) => SetState(state);
	public void Mock_OpenJournal() => IsJournalOpen = true;
	public void Mock_CloseJournal() => IsJournalOpen = false;
	public void Mock_OpenInventory() => IsInventoryOpen = true;
	public void Mock_CloseInventory() => IsInventoryOpen = false;

	#endregion
}
