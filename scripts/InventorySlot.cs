using Godot;


public partial class InventorySlot : Panel
{
	public int SlotIndex { get; set; }

	private TextureRect _icon;
	private Label _quantityLabel;
	private InventorySlotData _data;
	private Inventory _inventory;

	public override void _Ready()
	{
		_icon ??= GetNodeOrNull<TextureRect>("Icon");
		_quantityLabel ??= GetNodeOrNull<Label>("QuantityLabel");
		_inventory = GetNodeOrNull<Inventory>("/root/Inventory");
		if(_icon == null) GD.PrintErr($"[LỖI] Node 'Icon' không tồn tại trong ô '{Name}'!");
		if(_quantityLabel == null) GD.PrintErr($"[LỖI] Node 'QuantityLabel' không tồn tại trong ô '{Name}'!");
		GuiInput += OnGuiInput;
	}

	private Inventory GetInventory()
	{
		if (_inventory != null) return _inventory;
		_inventory = GetNodeOrNull<Inventory>("/root/Inventory");
		return _inventory;
	}

	public void UpdateSlot(InventorySlotData data)
	{
		_data = data;
		
		if (_icon == null) return;

		if (data == null || data.IsEmpty)
		{
			_icon.Texture = null;
			_icon.Visible = false;
			if (_quantityLabel != null) _quantityLabel.Visible = false;
		}
		else
		{
			_icon.Texture = data.Item.Icon;
			_icon.Visible = _icon.Texture != null;
			
			if (_quantityLabel != null)
			{
				_quantityLabel.Visible = data.Quantity > 1;
				_quantityLabel.Text = data.Quantity.ToString();
			}
		}
	}

	private void OnGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			var inventory = GetInventory();
			if (inventory == null) return;

			inventory.ActiveSlotIndex = SlotIndex;
			if (mb.ButtonIndex == MouseButton.Left)
			{
				inventory.UseItem(SlotIndex);
			}
			else if (mb.ButtonIndex == MouseButton.Right)
			{
				inventory.DropItem(SlotIndex, 1);
			}
		}
	}

	// ---------- Kéo - thả để sắp xếp lại túi đồ ----------

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (_data == null || _data.IsEmpty || _data.Item.Icon == null) return default;

		var preview = new TextureRect
		{
			Texture = _data.Item.Icon,
			CustomMinimumSize = new Vector2(48, 48),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		SetDragPreview(preview);

		return SlotIndex; 
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return data.VariantType == Variant.Type.Int;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		int fromIndex = data.AsInt32();
		var inventory = GetInventory();
		if (inventory != null)
		{
			inventory.SwapSlots(fromIndex, SlotIndex);
		}
	}

	public void SetHighlight(bool active)
	{
		SelfModulate = active ? new Color(1.5f, 1.5f, 1.1f) : new Color(1f, 1f, 1f);
	}
}
