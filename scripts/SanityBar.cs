using Godot;
using System;

public partial class SanityBar : ProgressBar
{
	// Called when the node enters the scene tree for the first time.
	private ProgressBar damageBar;
	private Timer timer;

	[Export] public float catchUpSpeed = 5f;
	private float targetSanity = 100f;
	public override void _Ready()
	{
		damageBar = GetNode<ProgressBar>("DamageBar");
		timer = GetNode<Timer>("Timer");

		Value = targetSanity;
		if (damageBar != null)
		{
			damageBar.Value = targetSanity;
		}

		if (timer != null)
		{
			timer.Timeout += OnTimerTimeout;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (damageBar != null && damageBar.Value > Value)
		{
			if (timer != null && timer.IsStopped())
			{
				damageBar.Value = Mathf.MoveToward(damageBar.Value, Value, (float)delta * catchUpSpeed * 20f);
			}
		}
	}

	public void UpdateSanity(float currSanity)
	{
		float prevSanity = (float)Value;
		targetSanity = currSanity;

		Value = targetSanity;

		if (targetSanity < prevSanity)
		{
			if (timer != null)
			{
				timer.Start();
			}
		}
		else
		{
			if (damageBar != null)
			{
				damageBar.Value = targetSanity;
			}
		}
	}

	private void OnTimerTimeout()
	{
		
	}
}
