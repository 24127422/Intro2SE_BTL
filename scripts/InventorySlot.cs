using Godot;

// Gắn script này vào scene InventorySlot.tscn (gốc là Panel)
// Cấu trúc scene gợi ý:
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
		_icon ??= GetNodeOrNull<TextureRect>("Icon");
		_quantityLabel ??= GetNodeOrNull<Label>("QuantityLabel"); //Tự động tiềm kiếm 
		if(_icon == null) GD.PrintErr($"[LỖI] Node 'Icon' không tồn tại trong ô '{Name}'!");
		if(_quantityLabel == null) GD.PrintErr($"[LỖI] Node 'QuantityLabel' không tồn tại trong ô '{Name}'!");
		GuiInput += OnGuiInput;
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
            if (mb.ButtonIndex == MouseButton.Left)
            {
                Inventory.Instance.UseItem(SlotIndex);
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                Inventory.Instance.DropItem(SlotIndex, 1);
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
        Inventory.Instance.SwapSlots(fromIndex, SlotIndex);
    }
}
