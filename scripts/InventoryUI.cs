using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
    [Export] public PackedScene SlotScene;
    [Export] public GridContainer SlotContainer;

    private List<InventorySlot> _slotNodes = new();

    public override void _Ready()
    {
        if (Inventory.Instance == null)
        {
            GD.PrintErr("[LỖI] Autoload 'Inventory' không tồn tại! Không thể kết nối UI.");
            return;
        }

        // Đăng ký sự kiện cập nhật UI
        Inventory.Instance.InventoryChanged += RefreshUI;
        
        BuildSlots();
        RefreshUI();
    }

    // QUAN TRỌNG: Phải hủy đăng ký sự kiện khi Node này bị xóa khỏi Scene Tree để tránh rò rỉ bộ nhớ và crash game
    public override void _ExitTree()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.InventoryChanged -= RefreshUI;
        }
    }

    private void BuildSlots()
    {
        if (SlotContainer == null)
        {
            GD.PrintErr($"[LỖI] Chưa kéo thả 'SlotContainer' (GridContainer) vào UI '{Name}'!");
            return;
        }
        if (SlotScene == null)
        {
            GD.PrintErr($"[LỖI] Chưa kéo thả 'SlotScene' (InventorySlot.tscn) vào UI '{Name}'!");
            return;
        }

        foreach (Node child in SlotContainer.GetChildren())
            child.QueueFree();
            
        _slotNodes.Clear();

        for (int i = 0; i < Inventory.Instance.Slots.Count; i++)
        {
            var slotNode = SlotScene.Instantiate<InventorySlot>();
            SlotContainer.AddChild(slotNode);
            slotNode.SlotIndex = i;
            _slotNodes.Add(slotNode);
        }
    }

    private void RefreshUI()
    {
        for (int i = 0; i < _slotNodes.Count; i++)
        {
            if (i < Inventory.Instance.Slots.Count)
            {
                _slotNodes[i].UpdateSlot(Inventory.Instance.Slots[i]);
            }
        }
    }
}