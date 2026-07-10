using Godot;
using System;

public partial class HungerBar : ProgressBar
{
    private ProgressBar damageBar;
    private Timer timer;

    [Export] public float catchUpSpeed = 5f;
    private float targetHunger = 100f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        damageBar = GetNode<ProgressBar>("DamageBar");
        timer = GetNode<Timer>("Timer");

        Value = targetHunger;
        if (damageBar != null)
        {
            damageBar.Value = targetHunger;
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

    public void UpdateHunger(float currentHunger)
    {
        float previousHunger = (float)Value;
        targetHunger = currentHunger;

        Value = targetHunger;


        if (targetHunger < previousHunger)
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
                damageBar.Value = targetHunger;
            }
        }
    }

    private void OnTimerTimeout()
    {
    }
}