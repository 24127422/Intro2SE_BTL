using Godot;
using System;

public partial class PlayerStats : Node
{
	[Export] public StatsControl MyStatsControl;

	private float maxHealth = 100f;
	private float currHealth = 100f;
	private float maxHunger = 100f;
	private float currHunger = 100f;
	private float maxThirst = 100f;
	private float currThirst = 100f;
	private float maxSanity = 100f;
	private float currSanity = 100f;

	[Export] public float HungerDecreaseRate = 0.0167f;
	[Export] public float ThirstDecreaseRate = 0.0167f;
	[Export] public float SanityDecreaseRate = 0.0167f;

	[Export] public float HungerZeroDamageRate = 2f;
	[Export] public float ThirstZeroDamageRate = 2f;
	[Export] public float SanityZeroDamageRate = 1f;
    private movement _player;

	public override void _Ready()
	{
		currHealth = maxHealth;
		currHunger = maxHunger;
		currThirst = maxThirst;
		currSanity = maxSanity;

        _player = GetParentOrNull<movement>();

		if (MyStatsControl != null)
		{
			MyStatsControl.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);
			MyStatsControl.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);
			MyStatsControl.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
			MyStatsControl.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
		}
	}

	public override void _Process(double delta)
	{
        if (_player != null && _player.IsDead)
        {
            return;
        }

		if (currHunger > 0)
		{
			float hungerRate = HungerDecreaseRate * (Input.IsActionPressed("sprint") ? 5f : 1f);
			currHunger = Mathf.Max(0, currHunger - (float)delta * hungerRate);
			MyStatsControl?.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);
		}
		else
		{
			TakeDamage((float)delta * HungerZeroDamageRate);
		}

		if (currThirst > 0)
		{
			float thirstRate = ThirstDecreaseRate * (Input.IsActionPressed("sprint") ? 5f : 1f);
			currThirst = Mathf.Max(0, currThirst - (float)delta * thirstRate);
			MyStatsControl?.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
		}
		else
		{
			TakeDamage((float)delta * ThirstZeroDamageRate);
		}

		if (currSanity > 0)
		{
			currSanity = Mathf.Max(0, currSanity - (float)delta * SanityDecreaseRate);
			MyStatsControl?.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
		}
		else
		{
			TakeDamage((float)delta * SanityZeroDamageRate);
		}
	}

        public void TakeDamage(float amount)
        {
            if (_player != null && _player.IsDead)
                return; // already dead, stop processing further damage

            _player?.FlashDamage();

            currHealth = Mathf.Max(0, currHealth - amount);
            MyStatsControl?.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);

            if (currHealth <= 0)
        {   
            _player?.Die();
            GD.Print("Player died. Game over!");
        }
}

	public void Consume(float hungerAmount, float thirstAmount = 0f, float sanityAmount = 0f, float healthAmount = 0f)
	{
		currHunger = Mathf.Min(maxHunger, currHunger + hungerAmount);
		MyStatsControl?.SetValue(ResourceType.Hunger, (int)currHunger, (int)maxHunger);

		if (thirstAmount != 0f)
		{
			currThirst = Mathf.Clamp(currThirst + thirstAmount, 0f, maxThirst);
			MyStatsControl?.SetValue(ResourceType.Thirst, (int)currThirst, (int)maxThirst);
		}

		if (sanityAmount != 0f)
		{
			currSanity = Mathf.Clamp(currSanity + sanityAmount, 0f, maxSanity);
			MyStatsControl?.SetValue(ResourceType.Sanity, (int)currSanity, (int)maxSanity);
		}

		if (healthAmount != 0f)
		{
			currHealth = Mathf.Clamp(currHealth + healthAmount, 0f, maxHealth);
			MyStatsControl?.SetValue(ResourceType.Health, (int)currHealth, (int)maxHealth);
		}

		GD.Print($"Consumed. Current Health is: {currHealth} | Hunger: {currHunger} | Thirst: {currThirst} | Sanity: {currSanity}");
	}
}
