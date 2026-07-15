using Godot;
using System;

public partial class movement : CharacterBody2D
{
	public float Speed = 150.0f;
	public float RunSpeed = 250.0f;
	private const int HotbarSize = 9;
	private Control _inventoryUI;
	private AnimatedSprite2D _sprite;
	private string _lastDirection = "S";

	[Export] public PackedScene ItemPickupScene;

	private Random _rng = new Random();

	private Vector2 GetDirection()
	{
		if (Input.IsActionPressed("left"))
			return Vector2.Left;

		if (Input.IsActionPressed("right"))
			return Vector2.Right;

		if (Input.IsActionPressed("up"))
			return Vector2.Up;

		if (Input.IsActionPressed("down"))
			return Vector2.Down;

		return Vector2.Zero;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = GetDirection();

		bool isBlocked = (DialogueUI.Instance.IsTalking) || (JournalUI.Instance.Visible);

		if (isBlocked)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

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
		_inventoryUI = GetNode<Control>("CanvasLayer/InventoryUI");

		Inventory.Instance.ItemDropped += OnItemDropped;
		// get sprite component
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
			{
				int targetSlot = (int)keyEvent.Keycode - (int)Key.Key1;
				SetActiveInventorySlot(targetSlot);
				GetViewport().SetInputAsHandled();
			}
		}

		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp)
			{
				ChangeActiveInventorySlot(-1);
				GetViewport().SetInputAsHandled();
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown)
			{
				ChangeActiveInventorySlot(1);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	public override void _Process(double delta)
	{
		// bật/tắt túi đồ
		if (Input.IsActionJustPressed("toggle_inventory"))
		{
			_inventoryUI.Visible = !_inventoryUI.Visible;
		}

		// bật/tắt journal
		if (Input.IsActionJustPressed("toggle_journal"))
		{
			JournalUI.Instance.Visible = !JournalUI.Instance.Visible;
		}
	}

	private void SetActiveInventorySlot(int slotIndex)
	{
		var inventory = GetNodeOrNull<Inventory>("/root/Inventory");
		if (inventory == null) return;

		int maxSlot = Mathf.Min(HotbarSize, inventory.Slots.Count) - 1;
		inventory.ActiveSlotIndex = Mathf.Clamp(slotIndex, 0, maxSlot);
	}

	private void ChangeActiveInventorySlot(int delta)
	{
		var inventory = GetNodeOrNull<Inventory>("/root/Inventory");
		if (inventory == null) return;

		int maxSlot = Mathf.Min(HotbarSize, inventory.Slots.Count) - 1;
		int currentSlot = Mathf.Clamp(inventory.ActiveSlotIndex, 0, maxSlot);
		int nextSlot = (currentSlot + delta + HotbarSize) % HotbarSize;
		if (nextSlot > maxSlot)
		{
			nextSlot = maxSlot;
		}
		inventory.ActiveSlotIndex = nextSlot;
	}

	private void OnItemDropped(Item item, int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			var pickup = ItemPickupScene.Instantiate<ItemPickup>();

			pickup.ItemData = item;

			float offsetX = (float)(_rng.NextDouble() * 20 - 10);
			float offsetY = (float)(_rng.NextDouble() * 20 - 10);
			Vector2 dropPosition = GlobalPosition + new Vector2(offsetX, offsetY);

			GetTree().CurrentScene.AddChild(pickup);
			pickup.GlobalPosition = dropPosition;
		}
	}
}
