using Godot;
using System.Collections.Generic;

public partial class TransitionRegistry : Node
{
	public static TransitionRegistry Instance { get; private set; }
	private readonly Dictionary<string, Door> _transitions = new();

	public override void _EnterTree() => Instance = this;

	public void RegisterTransition(string name, Door door)
	{
		if (!string.IsNullOrEmpty(name)) _transitions[name] = door;
	}

	public void UnregisterTransition(string name, Door door)
	{
		if (_transitions.TryGetValue(name, out var d) && d == door)
			_transitions.Remove(name);
	}

	public Vector2? GetWaypoint(string name)
		=> _transitions.TryGetValue(name, out var d) ? d.GetWaypoint() : null;
}
