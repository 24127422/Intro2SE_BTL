using Godot;

[GlobalClass]
public partial class AssemblyPart : Resource
{
	[Export] public Item RequiredItem { get; set; }
	[Export] public int RequiredQuantity { get; set; } = 1;
}
