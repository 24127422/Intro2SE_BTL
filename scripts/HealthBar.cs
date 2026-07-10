using Godot;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public partial class HealthBar : ProgressBar
{
	private ProgressBar damageBar;
	private Timer timer;

	[Export] public float catchUpSpeed = 5f;
	private float targetHealth = 100f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		damageBar = GetNode<ProgressBar>("DamageBar");
		timer = GetNode<Timer>("Timer");

		Value = targetHealth;
		if (damageBar != null)
		{
			damageBar.Value = targetHealth;
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

	public void UpdateHealth(float currentHealth)
	{
		float previousHealth = (float)Value;
		targetHealth = currentHealth;

		Value = targetHealth;

		if (targetHealth < previousHealth)
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
				damageBar.Value = targetHealth;
			}
		}
	}

	private void OnTimerTimeout()
	{
		
	}
}
