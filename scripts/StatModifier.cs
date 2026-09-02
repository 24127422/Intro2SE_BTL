// StatModifier.cs
using Godot;

public class StatModifier
{
    public string Source;
    public ResourceType Target;
    public float RateMultiplier = 1f;
    public float FlatBonus = 0f;
    public float Duration = -1f;
    public float TimeRemaining;
}