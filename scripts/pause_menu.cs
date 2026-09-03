using Godot;
using System;

public partial class pause_menu : CanvasLayer
{
    private Button resume_button;
    private Button save_button;
    private Button quit_button;

    [Export]
    public PackedScene save_menu_scene;

    [Export]
    public NodePath save_load_menu;

    private CanvasLayer saveLoadMenuInstance;
    private Node gameManager;
    private bool _sceneChangeRequested;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        Hide();

        resume_button = GetNodeOrNull<Button>("MenuPanel/VBoxContainer/ResumeBtn");
        save_button = GetNodeOrNull<Button>("MenuPanel/VBoxContainer/SaveBtn");
        quit_button = GetNodeOrNull<Button>("MenuPanel/VBoxContainer/QuitBtn");

        saveLoadMenuInstance = GetNodeOrNull<CanvasLayer>(save_load_menu);

        if (saveLoadMenuInstance == null && save_menu_scene == null)
        {
            save_menu_scene = GD.Load<PackedScene>("res://tscn/Save.tscn");
        }

        if (saveLoadMenuInstance == null && save_menu_scene != null)
        {
            var instance = save_menu_scene.Instantiate<CanvasLayer>();
            AddChild(instance);
            saveLoadMenuInstance = instance;
            saveLoadMenuInstance.Hide();
        }

        if (resume_button != null)
            resume_button.Pressed += _OnResumePressed;

        if (save_button != null)
            save_button.Pressed += _OnSavePressed;

        if (quit_button != null)
            quit_button.Pressed += _OnQuitPressed;

        gameManager = GetNodeOrNull<Node>("/root/GameManager");
        if (gameManager != null)
        {
            gameManager.Connect("GameStateChanged", Callable.From<int>(_OnGameStateChanged));
        }
    }

    public override void _ExitTree()
    {
        if (resume_button != null) resume_button.Pressed -= _OnResumePressed;
        if (save_button != null) save_button.Pressed -= _OnSavePressed;
        if (quit_button != null) quit_button.Pressed -= _OnQuitPressed;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEcho && keyEcho.Echo)
            return;

        if ((@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            || @event.IsActionPressed("ui_cancel")
            || @event.IsActionPressed("toggle_pause"))
        {
            if (gameManager != null)
            {
                var currentState = (int)gameManager.Get("CurrentState");
                if (currentState == 0 || currentState == 3 || currentState == 4)
                    return;

                gameManager.Call("TogglePause");
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void _OnResumePressed()
    {
        if (gameManager != null && (int)gameManager.Get("CurrentState") == 2)
            gameManager.Call("TogglePause");
    }

    private void _OnQuitPressed()
    {
        if (_sceneChangeRequested)
            return;

        _sceneChangeRequested = true;
        GetTree().Paused = false;
        Hide();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.MainMenu);
        }

        CallDeferred(nameof(ChangeToMainMenu));
    }

    private void ChangeToMainMenu()
    {
        GetTree().ChangeSceneToFile("res://tscn/main_menu.tscn");
    }

    private void _OnGameStateChanged(int newState)
    {
        if (newState == 2)
        {
            Show();
        }
        else
        {
            Hide();
            if (saveLoadMenuInstance != null)
                saveLoadMenuInstance.Hide();
        }
    }

    private void _OnSavePressed()
    {
        if (saveLoadMenuInstance != null)
        {
            if (saveLoadMenuInstance is Save_load_menu slm)
            {
                slm.OpenMenu(Save_load_menu.Mode.SAVE);
            }
        }
        else
        {
            GD.PrintErr("Save menu instance is not available.");
        }
    }
}