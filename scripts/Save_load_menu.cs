using Godot;
using System;

public partial class Save_load_menu : CanvasLayer
{
    public enum Mode { SAVE = 0, LOAD = 1 }

    [Export]
    public Mode mode = Mode.SAVE;

    [Export]
    public int total_slots = 20;

    private Label title_label;
    private VBoxContainer slot_list;
    private ColorRect overlay;
    private Button quick_save_button;
    private PanelContainer info_panel;
    private Label info_label;
    private bool _sceneChangeRequested;

    public override void _Ready()
    {
        title_label = GetNodeOrNull<Label>("UIContainer/HeaderBox/Title");
        slot_list = GetNodeOrNull<VBoxContainer>("UIContainer/MainPanel/Margin/ScrollContainer/SlotList");
        overlay = GetNodeOrNull<ColorRect>("Overlay");

        info_panel = GetNodeOrNull<PanelContainer>("UIContainer/InfoPanel");
        if (info_panel == null)
        {
            info_panel = new PanelContainer();
            info_panel.Name = "InfoPanel";
            info_panel.CustomMinimumSize = new Vector2(0, 140);

            var info_margin = new MarginContainer();
            info_margin.AddThemeConstantOverride("margin_left", 12);
            info_margin.AddThemeConstantOverride("margin_top", 12);
            info_margin.AddThemeConstantOverride("margin_right", 12);
            info_margin.AddThemeConstantOverride("margin_bottom", 12);

            info_label = new Label();
            info_label.Name = "InfoLabel";
            info_label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            info_label.CustomMinimumSize = new Vector2(0, 100);
            info_label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            info_label.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            info_label.Text = "No save selected";

            info_margin.AddChild(info_label);
            info_panel.AddChild(info_margin);

            var uiContainer = GetNodeOrNull<Control>("UIContainer");
            if (uiContainer != null)
            {
                uiContainer.AddChild(info_panel);
                info_panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
                info_panel.OffsetLeft = -220;
                info_panel.OffsetTop = 285;
                info_panel.OffsetRight = 220;
                info_panel.OffsetBottom = 310;
            }
        }
        else
        {
            info_label = GetNodeOrNull<Label>("UIContainer/InfoPanel/InfoLabel");
        }

        if (info_label == null)
        {
            info_label = new Label();
            info_label.Name = "InfoLabel";
            info_label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            info_label.Text = "No save selected";

            if (info_panel != null)
                info_panel.AddChild(info_label);
        }

        quick_save_button = GetNodeOrNull<Button>("UIContainer/HeaderBox/SaveGameButton");
        if (quick_save_button == null)
        {
            quick_save_button = new Button();
            quick_save_button.Name = "SaveGameButton";
            quick_save_button.Text = "Save Game";
            quick_save_button.CustomMinimumSize = new Vector2(120, 40);

            var hdr = GetNodeOrNull<HBoxContainer>("UIContainer/HeaderBox");
            if (hdr != null)
                hdr.AddChild(quick_save_button);
        }

        if (quick_save_button != null)
            quick_save_button.Pressed += _OnQuickSavePressed;

        if (overlay != null)
            overlay.Connect("gui_input", new Callable(this, nameof(_OnOverlayGuiInput)));

        GetTree().SceneChanged += _OnSceneChanged;

        OpenMenu(mode);
    }

    public override void _ExitTree()
    {
        if (quick_save_button != null)
            quick_save_button.Pressed -= _OnQuickSavePressed;

        if (GetTree() != null)
            GetTree().SceneChanged -= _OnSceneChanged;
    }

    public void OpenMenu(Mode p_mode)
    {
        mode = p_mode;
        Show();
        if (title_label != null)
        {
            title_label.Text = "";
            title_label.Visible = false;
        }
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        if (slot_list == null) return;

        foreach (Node child in slot_list.GetChildren())
            child.QueueFree();

        for (int i = 1; i <= total_slots; i++)
        {
            var slot_container = _CreateSlotUI(i);
            slot_list.AddChild(slot_container);
        }
    }

    private HBoxContainer _CreateSlotUI(int slot_index)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);

        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(0, 72);
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var saveUtil = GetNodeOrNull<Save_utils>("/root/SaveUtils");
        if (saveUtil != null)
            btn.Text = saveUtil.GetSlotInfoText(slot_index);
        else
            btn.Text = $"[Slot {slot_index.ToString("D2")}]";

        btn.Pressed += () =>
        {
            _OnSlotPressed(slot_index);
            ShowSaveInfo(slot_index);
        };
        hbox.AddChild(btn);

        if (saveUtil != null)
        {
            var data = saveUtil.ReadSlot(slot_index);
            if (data != null)
            {
                var del_btn = new Button();
                del_btn.CustomMinimumSize = new Vector2(48, 0);
                del_btn.Text = "X";
                del_btn.Pressed += () => _OnDeletePressed(slot_index);
                hbox.AddChild(del_btn);
            }
        }

        return hbox;
    }

    public void _OnSlotPressed(int slot_index)
    {
        var saveUtil = GetNodeOrNull<Save_utils>("/root/SaveUtils");

        if (mode == Mode.SAVE)
        {
            bool success = false;
            if (saveUtil != null)
            {
                var current_game_data = saveUtil.CaptureCurrentGame();
                success = saveUtil.WriteSlot(slot_index, current_game_data);
            }
            else
            {
                GD.Print("SaveUtils autoload not found");
            }

            if (success)
            {
                GD.Print("Đã lưu thành công vào Slot: ", slot_index);
                RefreshSlots();
            }
        }
        else if (mode == Mode.LOAD)
        {
            if (_sceneChangeRequested)
                return;

            SaveGameData data = null;
            if (saveUtil != null)
                data = saveUtil.ReadSlot(slot_index);

            if (data != null)
            {
                GD.Print($"Đang tải dữ liệu Slot: {slot_index}");

                var scenePath = data.ScenePath;
                if (string.IsNullOrWhiteSpace(scenePath))
                    scenePath = "res://tscn/test.tscn";

                _sceneChangeRequested = true;
                CloseMenu();
                GameManager.Instance?.SetState(GameManager.GameState.Playing);
                GetTree().Paused = false;

                if (saveUtil != null)
                {
                    data.ScenePath = scenePath;
                    saveUtil.QueueLoadData(data);
                }

                CallDeferred(nameof(ChangeToScene), scenePath);
            }
            else
            {
                GD.Print("Slot rỗng, không thể load!");
            }
        }

        ShowSaveInfo(slot_index);
    }

    public void _OnDeletePressed(int slot_index)
    {
        var saveUtil = GetNodeOrNull<Save_utils>("/root/SaveUtils");
        if (saveUtil != null)
            saveUtil.ClearSlot(slot_index);
        RefreshSlots();
        ShowSaveInfo(slot_index);
    }

    private void _OnQuickSavePressed()
    {
        var saveUtil = GetNodeOrNull<Save_utils>("/root/SaveUtils");
        if (saveUtil == null)
        {
            GD.PrintErr("[SaveMenu] SaveUtils autoload không tồn tại");
            return;
        }

        int nextSlot = saveUtil.GetNextAvailableSlotIndex();
        var data = saveUtil.CaptureCurrentGame();
        bool success = saveUtil.WriteSlot(nextSlot, data);
        if (success)
        {
            GD.Print($"[SaveMenu] Tạo save mới Slot {nextSlot} -> save_{nextSlot}.json");
            RefreshSlots();
            ShowSaveInfo(nextSlot);
        }
    }

    private void ShowSaveInfo(int slot_index)
    {
        var saveUtil = GetNodeOrNull<Save_utils>("/root/SaveUtils");
        var data = saveUtil?.ReadSlot(slot_index);

        if (info_label == null)
            return;

        if (data == null)
        {
            info_label.Text = $"Save slot: save_{slot_index}\nStatus: Empty\nFile: user://saves/save_{slot_index}.json";
            return;
        }

        info_label.Text = string.Join(
            "\n",
            new string[]
            {
                $"Save slot: save_{slot_index}",
                $"Created: {data.SaveTime}",
                $"Scene: {data.ScenePath}",
                $"Health: {data.Player?.Health ?? 0f:F0} | Hunger: {data.Player?.Hunger ?? 0f:F0} | Thirst: {data.Player?.Thirst ?? 0f:F0} | Sanity: {data.Player?.Sanity ?? 0f:F0}",
                $"Position: X={data.Player?.X ?? 0f:F1}, Y={data.Player?.Y ?? 0f:F1}",
                $"Inventory slots: {data.Inventory?.Slots?.Count ?? 0} | Journal records: {data.Journal?.UnlockedDocumentPaths?.Count ?? 0}",
                $"Ground items: {data.GroundItems?.Count ?? 0}"
            }
        );
    }

    public void _OnOverlayGuiInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton emb && emb.Pressed && emb.ButtonIndex == MouseButton.Left)
            CloseMenu();
    }

    public void CloseMenu()
    {
        Hide();
        EmitSignal("menu_closed");
    }

    private void ChangeToScene(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }

    private void _OnSceneChanged()
    {
        _sceneChangeRequested = false;
        Hide();
    }
}

