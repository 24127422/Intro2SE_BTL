using Godot;

public partial class MainMenu : CanvasLayer
{
    [Export] public string MainScenePath = "res://tscn/test.tscn";

    private Button _startButton;
    private Button _optionsButton;
    private Button _loadButton;
    private Button _quitButton;

    public override void _Ready()
    {
        _startButton = GetNodeOrNull<Button>("Panel/VBox/StartButton");
        _optionsButton = GetNodeOrNull<Button>("Panel/VBox/OptionsButton");
        _quitButton = GetNodeOrNull<Button>("Panel/VBox/QuitButton");

        _loadButton = GetNodeOrNull<Button>("Panel/VBox/LoadButton");
        if (_loadButton == null)
        {
            _loadButton = new Button();
            _loadButton.Name = "LoadButton";
            _loadButton.Text = "Load Game";
            _loadButton.CustomMinimumSize = new Vector2(0, 56);
            _loadButton.Pressed += OnLoadPressed;

            var box = GetNodeOrNull<VBoxContainer>("Panel/VBox");
            if (box != null)
            {
                box.AddChild(_loadButton);
            }
        }

        if (_startButton != null)
            _startButton.Pressed += OnStartPressed;

        if (_optionsButton != null)
            _optionsButton.Pressed += OnOptionsPressed;

        if (_loadButton != null)
            _loadButton.Pressed += OnLoadPressed;

        if (_quitButton != null)
            _quitButton.Pressed += OnQuitPressed;
    }

    private void OnStartPressed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }

        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://tscn/test.tscn");
    }

    private void OnLoadPressed()
    {
        var saveScene = GD.Load<PackedScene>("res://tscn/Save.tscn");
        if (saveScene == null)
        {
            GD.PrintErr("[MainMenu] Không tìm thấy scene save: res://tscn/Save.tscn");
            return;
        }

        var instance = saveScene.Instantiate<CanvasLayer>();
        if (instance is Save_load_menu slm)
        {
            slm.OpenMenu(Save_load_menu.Mode.LOAD);
        }

        AddChild(instance);
    }

    private void OnOptionsPressed()
    {

    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
};


