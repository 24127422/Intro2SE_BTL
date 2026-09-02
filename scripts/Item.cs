using Godot;

[GlobalClass] 
public partial class Item : Resource
{
	[ExportGroup("General Info")]
	[Export] public string ItemName { get; set; } = "";
	[Export] public Texture2D Icon { get; set; } 
	[Export] public Texture2D HorrorIcon { get; set; }
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string Thought { get; set; }
	
	[ExportGroup("Item Settings")]
	[Export] public bool IsConsumable { get; set; } = false; 
	[Export] public bool IsDocument { get; set; } = false;
	[Export] public int MaxStackSize { get; set; } = 5;
	[Export] public PackedScene HandModel { get; set; }

	[ExportGroup("Consumable Effects")]
	[Export] public float RestoreHunger { get; set; } = 0f;
	[Export] public float RestoreThirst { get; set; } = 0f;
	[Export] public float RestoreSanity { get; set; } = 0f;
	[Export] public float RestoreHealth { get; set; } = 0f;

	// === TÍNH NĂNG TÀI LIỆU (DOCUMENT) ===
	[ExportGroup("Document Details")]
	[Export] public string Author { get; set; } = "Unknown";
	[Export] public string Date { get; set; } = "";
	/// <summary> Danh sách các trang văn bản của tài liệu </summary>
	[Export(PropertyHint.MultilineText)] 
	public Godot.Collections.Array<string> Pages { get; set; } = new();
	[Export] public Texture2D DocumentImage { get; set; }
	[Export] public AudioStream PageTurnSound { get; set; }

	[ExportGroup("Directional Textures (Optional)")]
	[Export] public Texture2D TextureNorth { get; set; }
	[Export] public Texture2D TextureSouth { get; set; }
	[Export] public Texture2D TextureEast { get; set; }
	[Export] public Texture2D TextureWest { get; set; }
	
	[ExportGroup("Buff Effects")]
	[Export] public ResourceType BuffTarget { get; set; } = ResourceType.Stamina;
	[Export] public float BuffRateMultiplier { get; set; } = 1f;
	[Export] public float BuffFlatBonus { get; set; } = 0f;
	[Export] public float BuffDuration { get; set; } = 0f;

	[ExportGroup("Hold Effects")]
	[Export] public bool DrainsSanityWhileHeld { get; set; } = false;
	[Export] public float HeldSanityDrainMultiplier { get; set; } = 1f;
	public Texture2D GetDisplayIcon(float sanityPercent, float threshold = 40f)
	{
		if (HorrorIcon != null && sanityPercent <= threshold)
			return HorrorIcon;
		return Icon;
	}
}
