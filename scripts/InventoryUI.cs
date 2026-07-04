using Godot;
using System.Collections.Generic;

// Gắn script này vào node Panel "InventoryUI"
// GetNode<Panel>("CanvasLayer/InventoryUI"))
public partial class InventoryUI : Control
{
	// Kéo scene InventorySlot.tscn vào ô này trong Inspector
	[Export] public PackedScene SlotScene;

	// Kéo node GridContainer (nơi chứa các ô) vào đây trong Inspector
	[Export] public GridContainer SlotContainer;

	private List<InventorySlot> _slotNodes = new();

	public override void _Ready()
	{
		Inventory.Instance.InventoryChanged += RefreshUI;
		BuildSlots();
		RefreshUI();
	}

	private void BuildSlots()
	{
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
			_slotNodes[i].UpdateSlot(Inventory.Instance.Slots[i]);
		}
	}
}
