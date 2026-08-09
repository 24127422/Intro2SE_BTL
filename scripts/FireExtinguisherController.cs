using Godot;

public partial class FireExtinguisherController : Node
{
	[Export] public Node2D SprayOrigin;
	[Export] public PackedScene SprayScene;

	private float _timer = 0f;

	public override void _Process(double delta)
	{
		if (Inventory.Instance == null)
			return;

		int idx = Inventory.Instance.ActiveSlotIndex;

		if (idx < 0 || idx >= Inventory.Instance.Slots.Count)
			return;

		var slot = Inventory.Instance.Slots[idx];

		if (slot == null || slot.IsEmpty)
			return;

		if (slot.Item is not PrimaryItem primary)
			return;

		// Chỉ hoạt động với bình cứu hỏa
		if (primary.ItemName != "Bình cứu hỏa")
			return;

		// Không giữ chuột
		if (!Input.IsActionPressed("use_item"))
		{
			_timer = 0f;
			return;
		}
		
		float current = slot.CurrentDurability ?? primary.MaxDurability;
		// Hết durability
		if (primary.MaxDurability > 0f && current <= 0f)
		{
			_timer = 0f;
			return;
		}

		_timer -= (float)delta;

		// Xịt ngay lần đầu
		if (_timer <= 0f)
		{	
			SpawnSpray();

			// Trừ durability
			float durability = slot.CurrentDurability ?? primary.MaxDurability;

			slot.CurrentDurability = Mathf.Max(
				0f,
				durability - primary.DurabilityPerUse
			);

			// Cập nhật UI inventory
			Inventory.Instance.EmitSignal(Inventory.SignalName.InventoryChanged);

			_timer = primary.UseInterval;
		}
	}

	private void SpawnSpray()
	{
		if (SprayScene == null || SprayOrigin == null)
			return;

		var spray = SprayScene.Instantiate<ExtinguisherSpray>();

		GetTree().CurrentScene.AddChild(spray);

		spray.AttachTo(SprayOrigin);
	}
}
