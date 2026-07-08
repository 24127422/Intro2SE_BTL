using Godot;
using System;

public partial class movement : CharacterBody2D
{
	public float Speed = 300.0f;
	public float RunSpeed = 500.0f;
	private Control _inventoryUI;

	// Kéo scene ItemPickup.tscn vào đây trong Inspector
	[Export] public PackedScene ItemPickupScene;

	private Random _rng = new Random();

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		
		if (direction != Vector2.Zero) {
			float currentSpeed = Speed;

			if (Input.IsActionPressed("sprint"))
			{
				currentSpeed = RunSpeed;
			}

			velocity = direction * currentSpeed;
		} else {
			velocity = Vector2.Zero;
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	public override void _Ready()
	{
		// Đường dẫn tìm đến cái bảng UI nằm trong CanvasLayer
		_inventoryUI = GetNode<Control>("CanvasLayer/InventoryUI");

		// Lắng nghe sự kiện "item bị drop" từ túi đồ để spawn lại ItemPickup ngoài thế giới
		Inventory.Instance.ItemDropped += OnItemDropped;
	}

	public override void _Process(double delta)
	{
		// Logic bấm nút I để bật/tắt túi đồ
		if (Input.IsActionJustPressed("toggle_inventory"))
		{
			// Toogle
			_inventoryUI.Visible = !_inventoryUI.Visible;
		}
	}

	private void OnItemDropped(Item item, int amount)
	{
		if (ItemPickupScene == null)
		{
			GD.PrintErr("movement.cs: Chưa gán ItemPickupScene trong Inspector!");
			return;
		}

		for (int i = 0; i < amount; i++)
		{
			var pickup = ItemPickupScene.Instantiate<ItemPickup>();

			// Phải gán ItemData TRƯỚC khi AddChild, vì ItemPickup._Ready() 
			// dùng ItemData để set texture ngay khi vào scene tree
			pickup.ItemData = item;

			// Rải nhẹ ngẫu nhiên quanh chân nhân vật để nhiều item không đè lên nhau
			float offsetX = (float)(_rng.NextDouble() * 20 - 10);
			float offsetY = (float)(_rng.NextDouble() * 20 - 10);
			Vector2 dropPosition = GlobalPosition + new Vector2(offsetX, offsetY);

			GetTree().CurrentScene.AddChild(pickup);
			pickup.GlobalPosition = dropPosition;
		}
	}
}
