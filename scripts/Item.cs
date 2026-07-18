using Godot;

[GlobalClass] 
public partial class Item : Resource
{
	[Export] public string ItemName {get; set;} = "";
	
	[Export] public Texture2D Icon {get; set;} 
	
	[Export] public string Description {get; set;} = "";
	
	[Export] public string Thought {get; set;}
	
	[Export] public bool IsConsumable {get; set;} = false; 

	[Export] public bool IsDocument {get; set;} = false;

	[Export] public int MaxStackSize { get; set; } = 5;
	[Export] public PackedScene HandModel { get; set; }

	// === TÍCH HỢP THÊM VÀO ĐÂY ===
	[ExportGroup("Directional Textures (Optional)")]
	[Export] public Texture2D TextureNorth { get; set; }
	[Export] public Texture2D TextureSouth { get; set; }
	[Export] public Texture2D TextureEast { get; set; }
	[Export] public Texture2D TextureWest { get; set; }
}
