using Godot;
using System;
using Godot.Collections;

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

    public override void _Ready()
    {
        title_label = GetNodeOrNull<Label>("Title");
        slot_list = GetNodeOrNull<VBoxContainer>("SlotList");
        overlay = GetNodeOrNull<ColorRect>("Overlay");

        if (overlay != null)
            overlay.Connect("gui_input", new Callable(this, nameof(_OnOverlayGuiInput)));

        OpenMenu(mode);
    }

    public void OpenMenu(Mode p_mode)
    {
        mode = p_mode;
        Show();
        if (title_label != null)
            title_label.Text = (mode == Mode.SAVE) ? "SAVE GAME" : "LOAD GAME";
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

        var saveUtil = GetNodeOrNull<Save_utils>("/root/Save_utils");
        if (saveUtil != null)
            btn.Text = saveUtil.GetSlotInfoText(slot_index);
        else
            btn.Text = $"[Slot {slot_index.ToString("D2")}]";

        btn.Pressed += () => _OnSlotPressed(slot_index);
        hbox.AddChild(btn);

        if (saveUtil != null)
        {
            var data = saveUtil.ReadSlot(slot_index);
            if (data != null && data.Count > 0)
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
        var saveUtil = GetNodeOrNull<Save_utils>("/root/Save_utils");

        if (mode == Mode.SAVE)
        {
            var current_game_data = new Dictionary
            {
                { "player_hp", 100 },
                { "level_name", "Forest Area" },
                { "last_dialog", "Hero: Let's move on!" }
            };

            EmitSignal("save_requested", slot_index);

            bool success = false;
            if (saveUtil != null)
                success = saveUtil.WriteSlot(slot_index, current_game_data);
            else
                GD.Print("SaveUtils autoload not found");

            if (success)
            {
                GD.Print("Đã lưu thành công vào Slot: ", slot_index);
                RefreshSlots();
            }
        }
        else if (mode == Mode.LOAD)
        {
            Dictionary data = null;
            if (saveUtil != null)
                data = saveUtil.ReadSlot(slot_index);
            if (data != null && data.Count > 0)
            {
                GD.Print($"Đang tải dữ liệu Slot: {slot_index}");
                EmitSignal("load_requested", data);
                CloseMenu();
            }
            else
            {
                GD.Print("Slot rỗng, không thể load!");
            }
        }
    }

    public void _OnDeletePressed(int slot_index)
    {
        var saveUtil = GetNodeOrNull<Save_utils>("/root/Save_utils");
        if (saveUtil != null)
            saveUtil.ClearSlot(slot_index);
        RefreshSlots();
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
}
