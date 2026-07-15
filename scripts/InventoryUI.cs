using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
    [Export] public PackedScene SlotScene;
    [Export] public GridContainer SlotContainer;

    private List<InventorySlot> _slotNodes = new();
    private Inventory _inventory;

    public override void _Ready()
    {
        _inventory = GetNodeOrNull<Inventory>("/root/Inventory");
        if (_inventory == null)
        {
            GD.PrintErr("[LỖI] Autoload 'Inventory' không tồn tại! Không thể kết nối UI.");
            return;
        }

        // Đăng ký sự kiện cập nhật UI
        _inventory.InventoryChanged += RefreshUI;
        _inventory.ActiveSlotChanged += OnActiveSlotChanged;
        BuildSlots();
        RefreshUI();
    }

    // QUAN TRỌNG: Phải hủy đăng ký sự kiện khi Node này bị xóa khỏi Scene Tree để tránh rò rỉ bộ nhớ và crash game
    public override void _ExitTree()
    {
        if (_inventory != null)
        {
            _inventory.InventoryChanged -= RefreshUI;
            _inventory.ActiveSlotChanged -= OnActiveSlotChanged;
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

        for (int i = 0; i < _inventory.Slots.Count; i++)
        {
            var slotNode = SlotScene.Instantiate<InventorySlot>();
            SlotContainer.AddChild(slotNode);
            slotNode.SlotIndex = i;
            _slotNodes.Add(slotNode);
        }
    }

    private void RefreshUI()
    {
        if (_inventory == null) return;

        for (int i = 0; i < _slotNodes.Count; i++)
        {
            if (i < _inventory.Slots.Count)
            {
                _slotNodes[i].UpdateSlot(_inventory.Slots[i]);
                _slotNodes[i].SetHighlight(i == _inventory.ActiveSlotIndex);
            }
        }
    }
    private void OnActiveSlotChanged(int newIndex)
    {
        for (int i = 0; i < _slotNodes.Count; i++)
        {
            _slotNodes[i].SetHighlight(i == newIndex);
        }
    }
}