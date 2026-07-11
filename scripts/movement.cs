using Godot;
using System;

public partial class movement : CharacterBody2D
{
	public float Speed = 150.0f;
	public float RunSpeed = 250.0f;
	private Control _inventoryUI;
	private AnimatedSprite2D _sprite;
	private string _lastDirection = "S";
	private Vector2 _lastInput = Vector2.Zero;
	private int _leftOrder = -1;
	private int _rightOrder = -1;
	private int _upOrder = -1;
	private int _downOrder = -1;

	private int _inputCounter = 0;

	// Kéo scene ItemPickup.tscn vào đây trong Inspector
	[Export] public PackedScene ItemPickupScene;

	private Random _rng = new Random();
	
	private Vector2 GetDirection()
	{
		if (Input.IsActionJustPressed("left"))
			_leftOrder = ++_inputCounter;

		if (Input.IsActionJustPressed("right"))
			_rightOrder = ++_inputCounter;

		if (Input.IsActionJustPressed("up"))
			_upOrder = ++_inputCounter;

		if (Input.IsActionJustPressed("down"))
			_downOrder = ++_inputCounter;

		if (Input.IsActionJustReleased("left"))
			_leftOrder = -1;

		if (Input.IsActionJustReleased("right"))
			_rightOrder = -1;

		if (Input.IsActionJustReleased("up"))
			_upOrder = -1;

		if (Input.IsActionJustReleased("down"))
			_downOrder = -1;

		int best = -1;
		Vector2 dir = Vector2.Zero;

		if (Input.IsActionPressed("left") && _leftOrder > best)
		{
			best = _leftOrder;
			dir = Vector2.Left;
		}

		if (Input.IsActionPressed("right") && _rightOrder > best)
		{
			best = _rightOrder;
			dir = Vector2.Right;
		}

		if (Input.IsActionPressed("up") && _upOrder > best)
		{
			best = _upOrder;
			dir = Vector2.Up;
		}

		if (Input.IsActionPressed("down") && _downOrder > best)
		{
			best = _downOrder;
			dir = Vector2.Down;
		}

		return dir;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = GetDirection();

		if (direction != Vector2.Zero)
		{
			float currentSpeed = Input.IsActionPressed("sprint") ? RunSpeed : Speed;
			_sprite.SpeedScale = Input.IsActionPressed("sprint") ? 1.5f : 1.0f;
			
			bool blocked = TestMove(GlobalTransform, direction);

			if (direction == Vector2.Right)
			{
				_lastDirection = "E";
			}
			else if (direction == Vector2.Left)
			{
				_lastDirection = "W";
			}
			else if (direction == Vector2.Up)
			{
				_lastDirection = "N";
			}
			else if (direction == Vector2.Down)
			{
				_lastDirection = "S";
			}

			if (blocked)
			{
				Velocity = Vector2.Zero;

				string idleAnim = "Idle_" + _lastDirection;
				if (_sprite.Animation != idleAnim)
					_sprite.Play(idleAnim);
			}
			else
			{
				Velocity = direction * currentSpeed;

				string walkAnim = "Walk_" + _lastDirection;
				if (_sprite.Animation != walkAnim)
					_sprite.Play(walkAnim);
			}
		}
		else
		{
			Velocity = Vector2.Zero;
			_sprite.SpeedScale = 1.0f;

			string idleAnim = "Idle_" + _lastDirection;
			if (_sprite.Animation != idleAnim)
				_sprite.Play(idleAnim);
		}

		MoveAndSlide();
	}
	
	public override void _Ready()
	{
		// Đường dẫn tìm đến cái bảng UI nằm trong CanvasLayer
		_inventoryUI = GetNode<Control>("CanvasLayer/InventoryUI");

		// Lắng nghe sự kiện "item bị drop" từ túi đồ để spawn lại ItemPickup ngoài thế giới
		Inventory.Instance.ItemDropped += OnItemDropped;
		
		// get sprite component
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
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
