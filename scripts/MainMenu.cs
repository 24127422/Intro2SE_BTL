using Godot;

public partial class MainMenu : CanvasLayer
{
    [Export] public string MainScenePath = "res://tscn/test.tscn";

    private Button _startButton;
    private Button _optionsButton;
    private Button _quitButton;

    public override void _Ready()
    {
        _startButton = GetNodeOrNull<Button>("Panel/VBox/StartButton");
        _optionsButton = GetNodeOrNull<Button>("Panel/VBox/OptionsButton");
        _quitButton = GetNodeOrNull<Button>("Panel/VBox/QuitButton");
        
        if (_startButton != null)
            _startButton.Pressed += OnStartPressed;

        if (_optionsButton != null)
            _optionsButton.Pressed += OnOptionsPressed;

        if (_quitButton != null)
            _quitButton.Pressed += OnQuitPressed;
    }

    private void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://tscn/test.tscn");
    }

    private void OnOptionsPressed()
    {

    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
};


