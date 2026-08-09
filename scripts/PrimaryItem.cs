using Godot;

[GlobalClass]
public partial class PrimaryItem : Item
{
	// USE MODE
	[ExportGroup("Primary Item - Use Mode")]
	// true  = bật/tắt bằng 1 lần bấm (Flashlight)
	// false = chỉ hoạt động khi giữ nút sử dụng (Fire Extinguisher)
	[Export] public bool IsToggleable { get; set; } = true;

	// Chỉ dùng cho item giữ để sử dụng
	// Mỗi UseInterval giây sẽ kích hoạt 1 lần
	[Export] public float UseInterval { get; set; } = 0.1f;

	// Mỗi lần kích hoạt sẽ tiêu hao bao nhiêu durability
	[Export] public float DurabilityPerUse { get; set; } = 1f;

	// Với item toggle: khi vừa equip có bật sẵn không
	[Export] public bool StartsActive { get; set; } = false;


	// DURABILITY / ENERGY
	[ExportGroup("Primary Item - Durability")]

	// <= 0 nghĩa là dùng vô hạn
	[Export] public float MaxDurability { get; set; } = 100f;

	// Chỉ dùng cho item toggle (ví dụ: đèn pin đang bật)
	[Export] public float DrainPerSecond { get; set; } = 5f;

	[Export] public bool AutoDeactivateWhenEmpty { get; set; } = true;


	// EFFECTS
	[ExportGroup("Primary Item - Effects")]

	// Flashlight: Light2D
	// Extinguisher: AnimatedSprite2D / GPUParticles2D
	[Export] public PackedScene ActiveEffectScene { get; set; }


	// AUDIO
	[ExportGroup("Primary Item - Audio")]

	[Export] public AudioStream ActivateSound { get; set; }
	[Export] public AudioStream DeactivateSound { get; set; }
	[Export] public AudioStream DepletedSound { get; set; }


	// ICON STATES
	[ExportGroup("Primary Item - Icon States")]

	[Export] public Texture2D IconFull { get; set; }
	[Export] public Texture2D IconMedium { get; set; }
	[Export] public Texture2D IconLow { get; set; }
	[Export] public Texture2D IconOff { get; set; }


	// HELPERS
	public Texture2D GetIcon(float durability, bool active)
	{
		// Item đang tắt
		if (!active)
			return IconOff ?? Icon;

		// Item không dùng durability
		if (MaxDurability <= 0f)
			return IconFull ?? Icon;

		float p = durability / MaxDurability;

		if (p > 0.5f)
			return IconFull ?? Icon;

		if (p > 0.2f)
			return IconMedium ?? Icon;

		return IconLow ?? Icon;
	}
}
