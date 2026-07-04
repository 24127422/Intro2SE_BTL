using Godot;

// Cấu trúc scene:
// Panel (InventorySlot.cs)
//   └─ Icon (TextureRect)          -> tên node phải là "Icon"
//   └─ QuantityLabel (Label)       -> tên node phải là "QuantityLabel"
public partial class InventorySlot : Panel
{
	public int SlotIndex { get; set; }

	private TextureRect _icon;
	private Label _quantityLabel;
	private InventorySlotData _data;

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("Icon");
		_quantityLabel = GetNode<Label>("QuantityLabel");
		GuiInput += OnGuiInput;
	}

	public void UpdateSlot(InventorySlotData data)
	{
		_data = data;
		if (data.IsEmpty)
		{
			_icon.Texture = null;
			_icon.Visible = false;
			_quantityLabel.Visible = false;
		}
		else
		{
			_icon.Texture = data.Item.Icon;
			_icon.Visible = true;
			_quantityLabel.Visible = data.Quantity > 1;
			_quantityLabel.Text = data.Quantity.ToString();
		}
	}

	private void OnGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				// Click trái = dùng item
				Inventory.Instance.UseItem(SlotIndex);
			}
			else if (mb.ButtonIndex == MouseButton.Right)
			{
				// Click phải = vứt bớt 1 item
				Inventory.Instance.RemoveAt(SlotIndex, 1);
			}
		}
	}

// Kéo - thả 

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (_data == null || _data.IsEmpty) return default;

		var preview = new TextureRect
		{
			Texture = _data.Item.Icon,
			CustomMinimumSize = new Vector2(48, 48)
		};
		SetDragPreview(preview);

		return SlotIndex; // dữ liệu kéo đi chính là chỉ số ô nguồn
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return data.VariantType == Variant.Type.Int;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		int fromIndex = data.AsInt32();
		Inventory.Instance.SwapSlots(fromIndex, SlotIndex);
	}
}
