using Godot;
using System.Collections.Generic;

// Panel "kéo-thả ROM" hiện lên khi trạm lắp ráp đã đủ 7 bộ phận thân xác và đang
// chờ người chơi CHỌN 1 trong các ROM đang có trong túi đồ để kéo vào khe.
// Panel này TỰ VẼ 1 dải ô túi đồ ngay bên trong nó (mirror của Inventory.Instance.Slots)
// để nguồn kéo và đích thả nằm chung 1 CanvasLayer — không cần đụng tới hotbar thật ở
// dưới nữa, tránh mọi vấn đề mouse-filter xuyên lớp UI.
// Đăng ký làm Autoload (Singleton) tên "RomSlotUI" trong Project Settings > Autoload,
// giống DialogueUI / JournalUI.
public partial class RomSlotUI : CanvasLayer
{
	public static RomSlotUI Instance { get; private set; }

	[Export] public ColorRect Overlay;
	[Export] public Panel DropPanel;     // gắn script RomDropTarget.cs
	[Export] public TextureRect SlotIcon;
	[Export] public Label HintLabel;
	[Export] public Button CancelButton;

	[ExportGroup("Túi đồ nhúng trong panel")]
	[Export] public GridContainer InventorySlotsContainer;
	[Export] public PackedScene SlotScene; // res://tscn/InventorySlot.tscn

	private ComputerAssemblyStation _station;
	private readonly List<InventorySlot> _slotNodes = new();

	public override void _Ready()
	{
		Instance = this;
		Visible = false;

		if (Overlay != null)
			Overlay.GuiInput += OnOverlayGuiInput;

		if (CancelButton != null)
			CancelButton.Pressed += Close;

		if (Inventory.Instance != null)
			Inventory.Instance.InventoryChanged += OnInventoryChanged;
	}

	public override void _ExitTree()
	{
		if (Inventory.Instance != null)
			Inventory.Instance.InventoryChanged -= OnInventoryChanged;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible) return;
		if (@event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public bool IsOpen => Visible;

	public void Open(ComputerAssemblyStation station)
	{
		_station = station;
		Visible = true;

		if (SlotIcon != null)
			SlotIcon.Texture = null;

		if (HintLabel != null)
			HintLabel.Text = "Kéo 1 ROM từ dải ô bên trái vào khe bên phải";

		BuildSlots();

		// Chặn di chuyển/thao tác khác trong lúc đang chọn ROM — dùng chung cơ chế
		// với DialogueUI để không phải thêm state mới vào GameManager.
		GameManager.Instance?.StartDialogue();
	}

	public void Close()
	{
		Visible = false;
		_station = null;
		GameManager.Instance?.EndDialogue();
	}

	public bool IsValidRom(Item item)
	{
		if (_station == null || item == null) return false;
		foreach (var rom in _station.ValidRoms)
		{
			if (rom == item) return true;
		}
		return false;
	}

	// Được RomDropTarget._DropData() gọi khi người chơi thả 1 ô túi đồ vào khe
	public bool TryHandleDrop(int inventorySlotIndex)
	{
		if (_station == null) return false;

		var slots = Inventory.Instance?.Slots;
		if (slots == null || inventorySlotIndex < 0 || inventorySlotIndex >= slots.Count) return false;

		var slot = slots[inventorySlotIndex];
		if (slot == null || slot.IsEmpty) return false;

		bool success = _station.TryInsertSpecificRom(slot.Item);
		if (!success) return false;

		Inventory.Instance.RemoveAt(inventorySlotIndex, 1);
		Close();
		return true;
	}

	// ---------------- Dải ô túi đồ nhúng trong panel ----------------

	private void BuildSlots()
	{
		if (InventorySlotsContainer == null || SlotScene == null) return;

		foreach (Node child in InventorySlotsContainer.GetChildren())
			child.QueueFree();
		_slotNodes.Clear();

		var slots = Inventory.Instance?.Slots;
		if (slots == null) return;

		for (int i = 0; i < slots.Count; i++)
		{
			var slotNode = SlotScene.Instantiate<InventorySlot>();
			InventorySlotsContainer.AddChild(slotNode);
			slotNode.SlotIndex = i;
			_slotNodes.Add(slotNode);
		}

		RefreshSlots();
	}

	private void RefreshSlots()
	{
		var slots = Inventory.Instance?.Slots;
		if (slots == null) return;

		for (int i = 0; i < _slotNodes.Count && i < slots.Count; i++)
			_slotNodes[i].UpdateSlot(slots[i]);
	}

	private void OnInventoryChanged()
	{
		if (Visible)
			RefreshSlots();
	}

	private void OnOverlayGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			Close();
	}
}
