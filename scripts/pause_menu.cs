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

    public override void _Ready()
    {
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
            resume_button.Connect("pressed", new Callable(this, nameof(_OnResumePressed)));
        if (save_button != null)
            save_button.Connect("pressed", new Callable(this, nameof(_OnSavePressed)));
        if (quit_button != null)
            quit_button.Connect("pressed", new Callable(this, nameof(_OnQuitPressed)));

        if (Engine.HasSingleton("GameManager") || ClassDB.ClassExists("GameManager"))
        {
            var gm = GetNodeOrNull<Node>("/root/GameManager");
            if (gm != null)
                gm.Connect("GameStateChanged", new Callable(this, nameof(_OnGameStateChanged)));
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if ((@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            || @event.IsActionPressed("ui_cancel")
            || @event.IsActionPressed("toggle_pause"))
        {
            var gm = GetNodeOrNull<Node>("/root/GameManager");
            if (gm != null)
            {
                var current_state = (int)gm.Get("CurrentState");
                if (current_state == 0 || current_state == 3 || current_state == 4)
                    return;

                gm.Call("TogglePause");
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void _OnResumePressed()
    {
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm != null && (int)gm.Get("CurrentState") == 2)
            gm.Call("TogglePause");
    }

    public void _OnQuitPressed()
    {
        GetTree().Quit();
    }

    public void _OnGameStateChanged(int new_state)
    {
        if (new_state == 2)
            Show();
        else
        {
            Hide();
            if (saveLoadMenuInstance != null)
                saveLoadMenuInstance.Hide();
        }
    }

    public void _OnSavePressed()
    {
        if (saveLoadMenuInstance != null)
        {
            var slm = saveLoadMenuInstance as Save_load_menu;
            if (slm != null)
                slm.OpenMenu(Save_load_menu.Mode.SAVE);
        }
        else
        {
            GD.PrintErr("Save menu instance is not available.");
        }
    }
}
