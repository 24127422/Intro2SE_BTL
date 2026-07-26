using Godot;

[GlobalClass] 
public partial class Item : Resource
{
	[ExportGroup("General Info")]
	[Export] public string ItemName { get; set; } = "";
	[Export] public Texture2D Icon { get; set; } 
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string Thought { get; set; }
	
	[ExportGroup("Item Settings")]
	[Export] public bool IsConsumable { get; set; } = false; 
	[Export] public bool IsDocument { get; set; } = false;
	[Export] public int MaxStackSize { get; set; } = 5;
	[Export] public PackedScene HandModel { get; set; }

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
}