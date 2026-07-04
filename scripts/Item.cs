using Godot;

[GlobalClass] 
public partial class Item : Resource
{
	[Export] public string ItemName {get; set;} = "";
	
	[Export] public Texture2D Icon {get; set;} // Hình ảnh
	
	[Export] public string Description {get; set;} = "";
	
	[Export] public string Thought {get; set;}
	
	[Export] public bool IsConsumable {get; set;} = false; 

	// Số lượng tối đa gộp được trong 1 ô. 
	// = 1 nghĩa là item KHÔNG stack được (mỗi item chiếm 1 ô riêng)
	[Export] public int MaxStackSize {get; set;} = 5;
}
