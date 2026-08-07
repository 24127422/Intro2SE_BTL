using Godot;

// Áp dụng hành vi thời gian thực cho PrimaryItem (đèn pin, bình cứu hỏa...) đang ở ô
// ActiveSlotIndex: hao năng lượng mỗi frame khi Active, tự tắt khi hết, gắn/gỡ hiệu ứng
// hình ảnh (ActiveEffectScene), phát âm thanh Activate/Deactivate/Depleted.
//
// TÁCH RIÊNG khỏi PlayerHand.cs: PlayerHand chỉ lo HIỂN THỊ model/texture theo hướng nhìn,
// còn class này chỉ lo HÀNH VI (Active/Inactive, hao năng lượng, hiệu ứng). Việc BẬT/TẮT
// (IsActive) vẫn do Inventory.UseItem()/SetSlotActive() quyết định — Inventory vẫn là nguồn
// sự thật duy nhất; class này CHỈ lắng nghe & phản ứng, giống cách PlayerHand đang làm.
public partial class PrimaryItemController : Node
{
	// Điểm gắn hiệu ứng (Light2D, particle...) — thường trỏ tới HandPos/HandMarker của PlayerHand.
	[Export] public Node2D EffectAttachPoint;

	private Node2D _currentEffectNode;
	private AudioStreamPlayer2D _audioPlayer;
	private bool _wasActive = false;

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
		if (slot == null || primary == null || !slot.IsActive) return;
		if (primary.MaxDurability <= 0f) return; // <= 0 nghĩa là dùng vô hạn, không hao

		float current = (slot.CurrentDurability ?? primary.MaxDurability) - primary.DrainPerSecond * (float)delta;
		slot.CurrentDurability = Mathf.Max(0f, current);

		if (slot.CurrentDurability <= 0f && primary.AutoDeactivateWhenEmpty)
		{
			PlaySound(primary.DepletedSound);
			// Tắt qua Inventory (nguồn sự thật duy nhất) -> phát InventoryChanged
			// -> OnInventoryChanged bên dưới tự dọn hiệu ứng/âm thanh.
			Inventory.Instance.SetSlotActive(Inventory.Instance.ActiveSlotIndex, false);
		}
	}

	// ------------------------------------------------------------------
	// PHẢN ỨNG lại thay đổi từ Inventory (không tự ý bật/tắt gì ở đây)
	// ------------------------------------------------------------------

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

	// ------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------

	private void SpawnEffect(PrimaryItem primary)
	{
		ClearEffect();
		if (primary.ActiveEffectScene == null || EffectAttachPoint == null) return;

		_currentEffectNode = primary.ActiveEffectScene.Instantiate<Node2D>();
		EffectAttachPoint.AddChild(_currentEffectNode);
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
