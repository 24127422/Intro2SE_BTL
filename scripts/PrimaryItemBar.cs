using Godot;

// Thanh hiển thị năng lượng còn lại (pin đèn pin, áp suất bình cứu hỏa...) của
// PrimaryItem đang ở ô ActiveSlotIndex. Poll trực tiếp mỗi frame (đơn giản, đủ nhẹ)
// thay vì chờ signal, vì năng lượng hao dần liên tục trong PrimaryItemController._Process
// chứ không phát InventoryChanged mỗi frame (tránh spam signal).
// Tự ẩn khi ô đang chọn không có PrimaryItem, hoặc là dụng cụ dùng vô hạn (MaxDurability <= 0).
public partial class PrimaryItemBar : Control
{
	[Export] public ProgressBar Bar;
	[Export] public Label NameLabel;

	public override void _Ready()
	{
		Visible = false;
	}

	public override void _Process(double delta)
	{
		var inv = Inventory.Instance;
		if (inv == null) { Visible = false; return; }

		int idx = inv.ActiveSlotIndex;
		if (idx < 0 || idx >= inv.Slots.Count) { Visible = false; return; }

		var slot = inv.Slots[idx];
		if (slot.IsEmpty || slot.Item is not PrimaryItem primary || primary.MaxDurability <= 0f)
		{
			Visible = false;
			return;
		}

		Visible = true;
		float current = slot.CurrentDurability ?? primary.MaxDurability;

		if (Bar != null)
		{
			Bar.MaxValue = primary.MaxDurability;
			Bar.Value = current;
		}

		if (NameLabel != null)
		{
			int percent = Mathf.RoundToInt(current / primary.MaxDurability * 100f);
			string trangThai = slot.IsActive ? "Đang bật" : "Đang tắt";
			NameLabel.Text = $"{primary.ItemName} — {trangThai} ({percent}%)";
		}
	}
}
