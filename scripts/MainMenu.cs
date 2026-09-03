using Godot;

public partial class MainMenu : CanvasLayer
{
    [Export] public string MainScenePath = "res://tscn/test.tscn";

    private Button _startButton;
    private Button _optionsButton;
    private Button _loadButton;
    private Button _quitButton;
    private bool _sceneChangeRequested;

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
        if (_sceneChangeRequested)
            return;

        _sceneChangeRequested = true;
        GetTree().Paused = false;

        CallDeferred(nameof(ChangeToGameScene));
    }

    private void ChangeToGameScene()
    {
        // 1. Chuyển scene trước
        GetTree().ChangeSceneToFile(MainScenePath);
        
        // 2. Kích hoạt trạng thái Playing sau khi scene bắt đầu nạp
        GameManager.Instance?.StartGame();
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


