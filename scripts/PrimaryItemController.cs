using Godot;

public partial class PrimaryItemController : Node
{
	[Export] public Node2D EffectAttachPoint;

	private Node2D _currentEffectNode;
	private AudioStreamPlayer2D _audioPlayer;
	private bool _wasActive = false;
	private float _holdUseTimer = 0f;

	public override void _Ready()
	{
		_audioPlayer = new AudioStreamPlayer2D();
		AddChild(_audioPlayer);

		if (Inventory.Instance != null)
		{
			Inventory.Instance.InventoryChanged += OnInventoryChanged;
			Inventory.Instance.ActiveSlotChanged += OnActiveSlotChanged;
		}
	}

	public override void _ExitTree()
	{
		if (Inventory.Instance != null)
		{
			Inventory.Instance.InventoryChanged -= OnInventoryChanged;
			Inventory.Instance.ActiveSlotChanged -= OnActiveSlotChanged;
		}
	}

	public override void _Process(double delta)
	{
		var (slot, primary) = GetActivePrimary();

		if (slot == null || primary == null)
			return;

		// for toggle (Flashlight)
		if (primary.IsToggleable)
		{
			if (!slot.IsActive)
				return;

			if (primary.MaxDurability <= 0f)
				return;

			float current = (slot.CurrentDurability ?? primary.MaxDurability)
							- primary.DrainPerSecond * (float)delta;

			slot.CurrentDurability = Mathf.Max(0f, current);

			if (slot.CurrentDurability <= 0f && primary.AutoDeactivateWhenEmpty)
			{
				PlaySound(primary.DepletedSound);
				Inventory.Instance.SetSlotActive(Inventory.Instance.ActiveSlotIndex, false);
			}

			return;
		}

		// for hold (Fire Extinguisher)
		if (!Input.IsActionPressed("use_item"))
		{
			_holdUseTimer = 0f;
			return;
		}

		float durability = slot.CurrentDurability ?? primary.MaxDurability;

		if (primary.MaxDurability > 0f && durability <= 0f)
			return;

		_holdUseTimer += (float)delta;

		if (_holdUseTimer >= primary.UseInterval)
		{
			_holdUseTimer = 0f;

			SpawnEffect(primary);
			PlaySound(primary.ActivateSound);

			if (primary.MaxDurability > 0f)
			{
				slot.CurrentDurability = Mathf.Max(
					0f,
					durability - primary.DurabilityPerUse
				);
			}

			Inventory.Instance.EmitSignal(Inventory.SignalName.InventoryChanged);
		}
	}


	private void OnInventoryChanged()
	{
		SyncEffectAndSound();
	}

	private void OnActiveSlotChanged(int newIndex)
	{
		// Đổi ô hotbar -> luôn dọn hiệu ứng của item cũ trước, KHÔNG tự động bật item mới
		ClearEffect();
		_wasActive = false;
	}

	private void SyncEffectAndSound()
	{
		var (slot, primary) = GetActivePrimary();
		bool isActiveNow = slot != null && primary != null && slot.IsActive;

		if (isActiveNow == _wasActive) return; // trạng thái không đổi, khỏi làm gì thêm

		if (isActiveNow)
		{
			SpawnEffect(primary);
			PlaySound(primary.ActivateSound);
		}
		else
		{
			ClearEffect();
			if (primary != null) PlaySound(primary.DeactivateSound);
		}

		_wasActive = isActiveNow;
	}

	
private void SpawnEffect(PrimaryItem primary)
{
	// Luôn dọn hiệu ứng cũ TRƯỚC khi tạo mới — đảm bảo tại một thời điểm chỉ có
	// DUY NHẤT 1 effect node tồn tại, dù SpawnEffect được gọi lặp lại liên tục
	// (chế độ giữ nút, ví dụ bình cứu hỏa) hay chỉ gọi 1 lần (chế độ toggle, đèn pin).
	ClearEffect();

	if (primary.ActiveEffectScene == null || EffectAttachPoint == null)
		return;

	Node2D effect = primary.ActiveEffectScene.Instantiate<Node2D>();
	EffectAttachPoint.AddChild(effect);

	Vector2 mouseGlobal = GetViewport().GetMousePosition();
	Vector2 dir = (mouseGlobal - EffectAttachPoint.GlobalPosition).Normalized();

	effect.GlobalPosition = EffectAttachPoint.GlobalPosition;
	effect.GlobalRotation = dir.Angle();

	_currentEffectNode = effect;
}

	private void ClearEffect()
	{
		if (_currentEffectNode != null && GodotObject.IsInstanceValid(_currentEffectNode))
			_currentEffectNode.QueueFree();
		_currentEffectNode = null;
	}

	private void PlaySound(AudioStream stream)
	{
		if (stream == null || _audioPlayer == null) return;
		_audioPlayer.Stream = stream;
		_audioPlayer.Play();
	}

	private (InventorySlotData slot, PrimaryItem primary) GetActivePrimary()
	{
		var inv = Inventory.Instance;
		if (inv == null) return (null, null);

		int idx = inv.ActiveSlotIndex;
		if (idx < 0 || idx >= inv.Slots.Count) return (null, null);

		var slot = inv.Slots[idx];
		if (slot.IsEmpty || slot.Item is not PrimaryItem primary) return (slot, null);

		return (slot, primary);
	}
}
