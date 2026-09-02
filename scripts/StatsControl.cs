using Godot;
using System.Collections.Generic;

public enum ResourceType
{
	Health,
	Hunger,
	Thirst,
	Sanity,
	Stamina,
	Speed
}

public partial class StatsControl : Control
{
	[Export] public NodePath HealthProgressBarPath;
	[Export] public NodePath HealthValueLabelPath;
	[Export] public NodePath HealthNameLabelPath;

	[Export] public NodePath HungerProgressBarPath;
	[Export] public NodePath HungerValueLabelPath;
	[Export] public NodePath HungerNameLabelPath;

	[Export] public NodePath ThirstProgressBarPath;
	[Export] public NodePath ThirstValueLabelPath;
	[Export] public NodePath ThirstNameLabelPath;

	[Export] public NodePath SanityProgressBarPath;
	[Export] public NodePath SanityValueLabelPath;
	[Export] public NodePath SanityNameLabelPath;

	[Export] public NodePath StaminaProgressBarPath;
	[Export] public NodePath StaminaValueLabelPath;
	[Export] public NodePath StaminaNameLabelPath;

	private class BarRefs
	{
		public ProgressBar Bar;
		public Label ValueLabel;
		public Label NameLabel;
	}

	private readonly Dictionary<ResourceType, BarRefs> _bars = new();
	private readonly Dictionary<ResourceType, (int current, int max)> _values = new();

	public override void _Ready()
	{
		_bars[ResourceType.Health] = new BarRefs
		{
			Bar = GetNode<ProgressBar>(HealthProgressBarPath),
			ValueLabel = GetNode<Label>(HealthValueLabelPath),
			NameLabel = GetNode<Label>(HealthNameLabelPath)
		};
		_bars[ResourceType.Hunger] = new BarRefs
		{
			Bar = GetNode<ProgressBar>(HungerProgressBarPath),
			ValueLabel = GetNode<Label>(HungerValueLabelPath),
			NameLabel = GetNode<Label>(HungerNameLabelPath)
		};
		_bars[ResourceType.Thirst] = new BarRefs
		{
			Bar = GetNode<ProgressBar>(ThirstProgressBarPath),
			ValueLabel = GetNode<Label>(ThirstValueLabelPath),
			NameLabel = GetNode<Label>(ThirstNameLabelPath)
		};
		_bars[ResourceType.Sanity] = new BarRefs
		{
			Bar = GetNode<ProgressBar>(SanityProgressBarPath),
			ValueLabel = GetNode<Label>(SanityValueLabelPath),
			NameLabel = GetNode<Label>(SanityNameLabelPath)
		};

		_bars[ResourceType.Stamina] = new BarRefs
		{
			Bar = GetNode<ProgressBar>(StaminaProgressBarPath),
			ValueLabel = GetNode<Label>(StaminaValueLabelPath),
			NameLabel = GetNode<Label>(StaminaNameLabelPath)
		};



		_bars[ResourceType.Health].NameLabel.Text = "Health";
		_bars[ResourceType.Hunger].NameLabel.Text = "Hunger";
		_bars[ResourceType.Thirst].NameLabel.Text = "Thirst";
		_bars[ResourceType.Sanity].NameLabel.Text = "Sanity";
		_bars[ResourceType.Stamina].NameLabel.Text = "Stamina";

		// NOTE: no starting SetValue calls here anymore either —
		// PlayerStats._Ready() will push the initial values in, since
		// PlayerStats is the source of truth for the actual numbers now.
	}

	public void SetValue(ResourceType type, int current, int max)
	{
		current = Mathf.Clamp(current, 0, max);
		_values[type] = (current, max);

		var refs = _bars[type];
		refs.Bar.MaxValue = max;
		refs.Bar.Value = current;
		refs.ValueLabel.Text = $"{current}/{max}";
	}

	public int GetCurrent(ResourceType type) => _values[type].current;
	public int GetMax(ResourceType type) => _values[type].max;
}
