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

	[Export] public int MaxStackSize {get; set;} = 5;
}
