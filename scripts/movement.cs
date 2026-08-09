using System;
using Godot;

public partial class movement : CharacterBody2D
{
	public float Speed = 150.0f;
	public float RunSpeed = 200.0f;
	private const int HotbarSize = 9;
	private Control _inventoryUI;
	private AnimatedSprite2D _sprite;
	private string _lastDirection = "S";
	[Signal] public delegate void FacingDirectionChangedEventHandler(string direction);
	public string FacingDirection => _lastDirection;
	private bool _isFlashing = false;
	
	public bool IsDead { get; private set; } = false;

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
		if (IsDead)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (GameManager.Instance != null && GameManager.Instance.IsPlayerInputBlocked)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

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

			string newDirection = _lastDirection;
			if (direction == Vector2.Right)
			{
				newDirection = "E";
			}
			else if (direction == Vector2.Left)
			{
				newDirection = "W";
			}
			else if (direction == Vector2.Up)
			{
				newDirection = "N";
			}
			else if (direction == Vector2.Down)
			{
				newDirection = "S";
			}

			if (newDirection != _lastDirection)
			{
				_lastDirection = newDirection;
				EmitSignal(SignalName.FacingDirectionChanged, _lastDirection); 
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
		_inventoryUI = GetNodeOrNull<Control>("CanvasLayer/InventoryUI");
		if (_inventoryUI == null)
			GD.PrintErr("[movement] Không tìm thấy 'CanvasLayer/InventoryUI' dưới Player! Kiểm tra lại Player.tscn.");
		else
			_inventoryUI.Visible = true; 

		Inventory.Instance.ItemDropped += OnItemDropped;

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

		if (@event.IsActionPressed("toggle_useitem"))
		{
			UseActiveItem();
			GetViewport().SetInputAsHandled();
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
		if (maxSlot < 0) return;

		int slotCount = maxSlot + 1;
		int currentSlot = Mathf.Clamp(inventory.ActiveSlotIndex, 0, maxSlot);

		int nextSlot = ((currentSlot + delta) % slotCount + slotCount) % slotCount;

		inventory.ActiveSlotIndex = nextSlot;
	}

	private void UseActiveItem()
	{
		if (IsDead) return;
		if (DialogueUI.Instance != null && DialogueUI.Instance.IsTalking) return;
		if (JournalUI.Instance != null && JournalUI.Instance.Visible) return;

		var inventory = GetNodeOrNull<Inventory>("/root/Inventory");
		if (inventory == null) return;

		inventory.UseItem(inventory.ActiveSlotIndex);
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
	public void Die()
	{
		if (IsDead)
			return;

		IsDead = true;

		Velocity = Vector2.Zero;

		SetPhysicsProcess(false);
		SetProcessInput(false);
		SetProcessUnhandledInput(false);

		_sprite.Play("Death_" + _lastDirection);
	}
	
	public async void FlashDamage()
	{
		if (_isFlashing)
			return;

		_isFlashing = true;

		Color damageColor = new Color(1.0f, 0.5f, 0.5f);
		float duration = 0.25f;
		int steps = 30;

		for (int i = 0; i <= steps; i++)
		{
			float t = (float)i / steps;

			_sprite.Modulate = damageColor.Lerp(Colors.White, t);

			await ToSignal(GetTree().CreateTimer(duration / steps),
				SceneTreeTimer.SignalName.Timeout);
		}

		_sprite.Modulate = Colors.White;
		_isFlashing = false;
	}
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("toggle_inventory"))
		{
			bool newState = !_inventoryUI.Visible;
			GameManager.Instance?.SetInventoryOpen(newState);
			_inventoryUI.Visible = GameManager.Instance?.IsInventoryOpen ?? newState;
		}

		if (Input.IsActionJustPressed("toggle_journal"))
		{
			bool newState = !JournalUI.Instance.Visible;
			GameManager.Instance?.SetJournalOpen(newState);
			JournalUI.Instance.Visible = GameManager.Instance?.IsJournalOpen ?? newState;
		}
	}
}
