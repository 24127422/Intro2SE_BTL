using Godot;

public partial class GameOverUI : CanvasLayer
{
	private Button _retryButton;
	private Button _quitButton;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		Visible = false;

		_retryButton = GetNodeOrNull<Button>("Panel/VBox/RetryButton");
		_quitButton  = GetNodeOrNull<Button>("Panel/VBox/QuitButton");

		if (_retryButton == null)
			GD.PrintErr("[GameOverUI] Không tìm thấy RetryButton tại Panel/VBox/RetryButton");
		if (_quitButton == null)
			GD.PrintErr("[GameOverUI] Không tìm thấy QuitButton tại Panel/VBox/QuitButton");

		if (_retryButton != null) _retryButton.Pressed += OnRetryPressed;
		if (_quitButton  != null) _quitButton.Pressed  += OnQuitPressed;

		if (GameManager.Instance != null)
			GameManager.Instance.GameStateChanged += OnGameStateChanged;
		else
			GD.PrintErr("[GameOverUI] GameManager.Instance null lúc _Ready — kiểm tra thứ tự Autoload (GameManager phải đứng TRƯỚC GameOverUI).");
	}

	public override void _ExitTree()
	{
		if (_retryButton != null) _retryButton.Pressed -= OnRetryPressed;
		if (_quitButton  != null) _quitButton.Pressed  -= OnQuitPressed;

		if (GameManager.Instance != null)
			GameManager.Instance.GameStateChanged -= OnGameStateChanged;
	}

	private void OnGameStateChanged(int newState)
	{
		Visible = (GameManager.GameState)newState == GameManager.GameState.GameOver;
	}

	private void OnRetryPressed()
	{
		GetTree().Paused = false;
		GameManager.Instance?.SetState(GameManager.GameState.Playing);
		GetTree().ReloadCurrentScene();
	}

	private void OnQuitPressed()
	{
		GetTree().Paused = false;
		GameManager.Instance?.SetState(GameManager.GameState.MainMenu);
		GetTree().ChangeSceneToFile("res://tscn/main_menu.tscn");
	}
}
