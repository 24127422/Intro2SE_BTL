using Godot;



public partial class PlayerHand : Node2D
{
    [Export] public Node2D HandMarker { get; set; }
    private Node2D _currentHeldNode = null;
    private const int HotbarSize = 9;
    public override void _Ready()
    {
        if (HandMarker == null)
        {
            HandMarker = this;
        }
        if (Inventory.Instance != null)
        {
            Inventory.Instance.InventoryChanged += UpdateHeldItem;
            Inventory.Instance.ActiveSlotChanged += OnActiveSlotChanged;
        }
        UpdateHeldItem();
    }
    public override void _ExitTree()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.InventoryChanged -= UpdateHeldItem;
            Inventory.Instance.ActiveSlotChanged -= OnActiveSlotChanged;
        }
    }
    public override void _UnhandledInput(InputEvent @event)
	{
		// 1. Nhận phím bấm từ 1 đến 9 để đổi ô 
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
			{
				int targetSlot = (int)keyEvent.Keycode - (int)Key.Key1; // Phím 1 -> Slot 1
				Inventory.Instance.ActiveSlotIndex = targetSlot;
			}
		}

		// 2. Nhận cuộn chuột để di chuyển ô chọn nhanh 
		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp)
			{
				int prevSlot = (Inventory.Instance.ActiveSlotIndex - 1 + HotbarSize) % HotbarSize;
				Inventory.Instance.ActiveSlotIndex = prevSlot;
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown)
			{
				int nextSlot = (Inventory.Instance.ActiveSlotIndex + 1) % HotbarSize;
				Inventory.Instance.ActiveSlotIndex = nextSlot;
			}
		}
	}
    private void UpdateHeldItem()
	{
		// 1. DỌN DẸP: Xóa vật phẩm cũ đang cầm trên tay
		if (_currentHeldNode != null && GodotObject.IsInstanceValid(_currentHeldNode))
		{
			_currentHeldNode.QueueFree();
			_currentHeldNode = null;
		}

		if (Inventory.Instance == null || HandMarker == null) return;

		int activeIndex = Inventory.Instance.ActiveSlotIndex;
		if (activeIndex < 0 || activeIndex >= Inventory.Instance.Slots.Count) return;

		var activeSlot = Inventory.Instance.Slots[activeIndex];

		// Nếu ô được chọn đang trống
		if (activeSlot == null || activeSlot.IsEmpty || activeSlot.Item == null)
		{
			return;
		}

		Item item = activeSlot.Item;
		if (item.HandModel != null)
		{
			Node2D modelInstance = item.HandModel.Instantiate<Node2D>();
			HandMarker.AddChild(modelInstance);
			modelInstance.Position = Vector2.Zero;
			modelInstance.Rotation = 0f;
			_currentHeldNode = modelInstance;
		}
		else if (item.Icon != null)
		{
			Sprite2D fallbackSprite = new Sprite2D();
			fallbackSprite.Texture = item.Icon;
			HandMarker.AddChild(fallbackSprite);
			fallbackSprite.Position = Vector2.Zero;
			fallbackSprite.Rotation = 0f;
			_currentHeldNode = fallbackSprite;
		}
	}
    private void OnActiveSlotChanged(int newIndex)
	{
		UpdateHeldItem();
	}
}
