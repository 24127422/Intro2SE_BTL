using Godot;
using System;

public partial class movement : CharacterBody2D
{
	public float Speed = 300.0f;
	public float RunSpeed = 500.0f;
	private Panel _inventoryUI;
	
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
		_inventoryUI = GetNode<Panel>("CanvasLayer/InventoryUI");
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
}
