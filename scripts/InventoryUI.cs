using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
	[Export] public PackedScene SlotScene;
	[Export] public GridContainer SlotContainer;

	private List<InventorySlot> _slotNodes = new();
	private Inventory _inventory;

	private float _sanityCheckTimer = 0f;
	private const float SanityCheckInterval = 0.4f;

	public override void _Ready()
	{
		_inventory = GetNodeOrNull<Inventory>("/root/Inventory");
		if (_inventory == null)
		{
			GD.PrintErr("[LỖI] Autoload 'Inventory' không tồn tại! Không thể kết nối UI.");
			return;
		}

		_inventory.InventoryChanged += RefreshUI;
		_inventory.ActiveSlotChanged += OnActiveSlotChanged;
		BuildSlots();
		RefreshUI();
	}

	public override void _Process(double delta)
	{
		_sanityCheckTimer += (float)delta;
		if (_sanityCheckTimer >= SanityCheckInterval)
		{
			_sanityCheckTimer = 0f;
			RefreshUI();
		}
	}

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

		SlotContainer.Columns = Mathf.Max(1, _inventory.Slots.Count);

		for (int i = 0; i < _inventory.Slots.Count; i++)
		{
			var slotNode = SlotScene.Instantiate<InventorySlot>();
			SlotContainer.AddChild(slotNode);
			slotNode.SlotIndex = i;
			_slotNodes.Add(slotNode);
		}

		CallDeferred(nameof(UpdatePanelSize));
	}

	private void UpdatePanelSize()
	{
		if (SlotContainer == null) return;

		Vector2 needed = SlotContainer.GetCombinedMinimumSize();
		const float padding = 8f;

		float halfWidth = (needed.X + padding) / 2f;
		OffsetLeft = -halfWidth;
		OffsetRight = halfWidth;

		OffsetTop = -(needed.Y + padding);
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
