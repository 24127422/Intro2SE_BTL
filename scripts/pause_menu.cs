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

    public override void _Ready()
    {
        // 1. Tự động bật tính năng hoạt động khi Game bị Pause
        ProcessMode = ProcessModeEnum.Always;

        Hide();

        // Lấy tham chiếu các nút bấm
        resume_button = GetNodeOrNull<Button>("MenuPanel/VBoxContainer/ResumeBtn");
        save_button = GetNodeOrNull<Button>("MenuPanel/VBoxContainer/SaveBtn");
        quit_button = GetNodeOrNull<Button>("MenuPanel/VBoxContainer/QuitBtn");

        // Khởi tạo hoặc tìm Save/Load Menu Instance
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

        // 2. Đăng ký sự kiện (Signal) chuẩn C# Event của Godot 4
        if (resume_button != null)
            resume_button.Pressed += _OnResumePressed;

        if (save_button != null)
            save_button.Pressed += _OnSavePressed;

        if (quit_button != null)
            quit_button.Pressed += _OnQuitPressed;

        // 3. Cache tham chiếu GameManager từ Autoload
        gameManager = GetNodeOrNull<Node>("/root/GameManager");
        if (gameManager != null)
        {
            gameManager.Connect("GameStateChanged", Callable.From<int>(_OnGameStateChanged));
        }
    }

    public override void _ExitTree()
    {
        // Gỡ đăng ký Event khi Node bị giải phóng để tránh rò rỉ bộ nhớ
        if (resume_button != null) resume_button.Pressed -= _OnResumePressed;
        if (save_button != null) save_button.Pressed -= _OnSavePressed;
        if (quit_button != null) quit_button.Pressed -= _OnQuitPressed;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Chống lặp lệnh khi đè giữ phím ESC (Echo Key)
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
        GetTree().Quit();
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