using Godot;
using System;

public partial class PlayerStats : Node
{
	// === Singleton — cần thiết vì Inventory.UseItem() gọi thẳng
	// PlayerStats.Instance?.Consume(...) khi dùng item tiêu hao. ===
	public static PlayerStats Instance { get; private set; }
	

	[Export] public StatsControl MyStatsControl;

	private float maxHealth = 100f;
	private float currHealth = 100f;
	private float maxHunger = 100f;
	private float currHunger = 100f;
	private float maxThirst = 100f;
	private float currThirst = 100f;
	private float maxSanity = 100f;
	private float currSanity = 100f;
	private float maxStamina = 10f;
	private float currStamina = 10f;

	[Export] public float HungerDecreaseRate = 0.5f;
	[Export] public float ThirstDecreaseRate = 0.5f;
	[Export] public float SanityDecreaseRate = 0.5f;

	[Export] public float HungerZeroDamageRate = 2f;
	[Export] public float ThirstZeroDamageRate = 2f;
	[Export] public float SanityZeroDamageRate = 1f;

	[Export] public float StaminaDrainRate = 2f;
	[Export] public float StaminaRegenRate = 2f;
	
	private movement _player;
	private bool _staminaExhausted = false;

	// === Getter đọc-only cho Enemy Tactical AI (EnemyNeuralPolicy) lấy % máu thật
	// của người chơi. Enemy.cs phụ thuộc trực tiếp vào field này — KHÔNG được xóa
	// khi nâng cấp PlayerStats, nếu không Enemy.cs sẽ lỗi build ngược lại. ===
	public float HealthPercent => maxHealth > 0 ? currHealth / maxHealth * 100f : 100f;
	public float SanityPercent => maxSanity > 0 ? currSanity / maxSanity * 100f : 100f;
	// === Dùng bởi Save_utils.CaptureCurrentGame() để đóng gói snapshot lưu game ===
	public float GetCurrentHealth() => currHealth;
	public float GetCurrentHunger() => currHunger;
	public float GetCurrentThirst() => currThirst;
	public float GetCurrentSanity() => currSanity;

	// === Dùng bởi movement.cs để quyết định có cho sprint hay không ===
	public bool CanSprint => currStamina > 0 && !_staminaExhausted;

	// === Dùng bởi Save_utils.ApplySaveData() khi load game ===
	public void ApplySnapshot(PlayerSaveSnapshot snapshot)
	{
		if (snapshot == null) return;

		currHealth = Mathf.Clamp(snapshot.Health, 0f, maxHealth);
		currHunger = Mathf.Clamp(snapshot.Hunger, 0f, maxHunger);
		currThirst = Mathf.Clamp(snapshot.Thirst, 0f, maxThirst);
		currSanity = Mathf.Clamp(snapshot.Sanity, 0f, maxSanity);

		MyStatsControl?.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);
		MyStatsControl?.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);
		MyStatsControl?.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
		MyStatsControl?.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
	}

	public override void _Ready()
	{
		Instance = this;

		currHealth = maxHealth;
		currHunger = maxHunger;
		currThirst = maxThirst;
		currSanity = maxSanity;
		currStamina = maxStamina;

		_player = GetParentOrNull<movement>();

		if (MyStatsControl != null)
		{
			MyStatsControl.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);
			MyStatsControl.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);
			MyStatsControl.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
			MyStatsControl.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
			MyStatsControl.SetValue(ResourceType.Stamina, (int)currStamina, (int)maxStamina);
		}
	}

	public override void _Process(double delta)
{
	if (_player != null && _player.IsDead)
	{
		return;
	}

	// === Tính isSprinting TRƯỚC, dùng chung cho Hunger/Thirst/Stamina.
	// Trước đây Hunger/Thirst chỉ check Input.IsActionPressed("sprint") mà không
	// check isMoving -> đứng yên giữ phím sprint vẫn bị hao đói/khát x5 vô lý. ===
	bool wantsSprint = Input.IsActionPressed("sprint");
	bool isMoving = Input.IsActionPressed("left") || Input.IsActionPressed("right")
				|| Input.IsActionPressed("up") || Input.IsActionPressed("down");

	bool isSprinting = wantsSprint && isMoving && currStamina > 0 && !_staminaExhausted;

	if (currHunger > 0)
	{
		float hungerRate = HungerDecreaseRate * (isSprinting ? 5f : 1f);
		currHunger = Mathf.Max(0, currHunger - (float)delta * hungerRate);
		MyStatsControl?.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);
	}
	else
	{
		TakeDamage((float)delta * HungerZeroDamageRate);
	}

	if (currThirst > 0)
	{
		float thirstRate = ThirstDecreaseRate * (isSprinting ? 5f : 1f);
		currThirst = Mathf.Max(0, currThirst - (float)delta * thirstRate);
		MyStatsControl?.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
	}
	else
	{
		TakeDamage((float)delta * ThirstZeroDamageRate);
	}

	if (currSanity > 0)
	{
		currSanity = Mathf.Max(0, currSanity - (float)delta * SanityDecreaseRate);
		MyStatsControl?.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
	}
	else
	{
		TakeDamage((float)delta * SanityZeroDamageRate);
	}

	// === Stamina: hao khi sprint + đang di chuyển, hồi khi không sprint.
	// Kiệt sức (_staminaExhausted) thì phải hồi ĐẦY mới được sprint lại. ===
	if (isSprinting)
	{
		currStamina = Mathf.Max(0, currStamina - (float)delta * StaminaDrainRate);
		if (currStamina <= 0)
		{
			_staminaExhausted = true;
		}
	}
	else
	{
		currStamina = Mathf.Min(maxStamina, currStamina + (float)delta * StaminaRegenRate);
		if (_staminaExhausted && currStamina >= maxStamina)
		{
			_staminaExhausted = false;
		}
	}

	MyStatsControl?.SetValue(ResourceType.Stamina, (int)currStamina, (int)maxStamina);
}

	public void TakeDamage(float amount)
	{
		if (_player != null && _player.IsDead)
			return; // đã chết rồi, không xử lý damage thêm

		_player?.FlashDamage();

		currHealth = Mathf.Max(0, currHealth - amount);
		MyStatsControl?.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);

		if (currHealth <= 0)
		{
			_player?.Die();
			// Nối luồng Game Over còn thiếu (Phần 6.1 trong tài liệu kiến trúc):
			// trước đây chỉ Print ra console chứ không chuyển GameState.
			GameManager.Instance?.TriggerGameOver();
			GD.Print("Player died. Game over!");
		}
	}

	public void Consume(float hungerAmount, float thirstAmount = 0f, float sanityAmount = 0f, float healthAmount = 0f)
	{
		currHunger = Mathf.Min(maxHunger, currHunger + hungerAmount);
		MyStatsControl?.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);

		if (thirstAmount != 0f)
		{
			currThirst = Mathf.Clamp(currThirst + thirstAmount, 0f, maxThirst);
			MyStatsControl?.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
		}

		if (sanityAmount != 0f)
		{
			currSanity = Mathf.Clamp(currSanity + sanityAmount, 0f, maxSanity);
			MyStatsControl?.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
		}

		if (healthAmount != 0f)
		{
			currHealth = Mathf.Clamp(currHealth + healthAmount, 0f, maxHealth);
			MyStatsControl?.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);
		}

		GD.Print($"Consumed. Current Health is: {currHealth} | Hunger: {currHunger} | Thirst: {currThirst} | Sanity: {currSanity}");
	}
}
