using Godot;
using System;
using System.ComponentModel;

public partial class PlayerStats : Node
{
	// Called when the node enters the scene tree for the first time.
	[Export] public HealthBar MyHealthBar;
	[Export] public HungerBar MyHungerBar;
	[Export] public SanityBar MySanityBar;
	private float maxHealth = 100f;
	private float currHealth = 100f;
	private float maxHunger =  100f;
	private float currHunger = 100f;
	private float maxSanity = 100f;
	private float currSanity = 100f;

	[Export] public float HungerDecreaseRate = 1.0f;
	[Export] public float SanityDecreaseRate = 0.5f;

	public override void _Ready()
	{
		currHealth = maxHealth;
		currHunger = maxHunger;
		currSanity = maxSanity;

		if (MyHealthBar != null) MyHealthBar.UpdateHealth(currHealth);
		if (MyHungerBar != null) MyHungerBar.UpdateHunger(currHunger);
		if (MySanityBar != null) MySanityBar.UpdateSanity(currSanity);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (currHunger > 0)
		{	
			float hungerRate = HungerDecreaseRate * (Input.IsActionPressed("sprint") ? 5f : 1f);
			currHunger = Mathf.Max(0, currHunger - (float)delta * hungerRate);

			if (MyHungerBar != null)
			{
				MyHungerBar.UpdateHunger(currHunger);
			}
		}
		else
		{
			TakeDamage((float)delta *2f);
		}

		if (currSanity > 0)
		{
			currSanity = Mathf.Max(0, currSanity - (float)delta * SanityDecreaseRate);

			if (MySanityBar != null)
			{
				MySanityBar.UpdateSanity(currSanity);
			}
		}
		else
		{
			TakeDamage((float)delta * 1f);
		}
	}

	public void TakeDamage(float amount)
	{
		currHealth = Mathf.Max(0, currHealth - amount);

		if (MyHealthBar != null)
		{
			MyHealthBar.UpdateHealth(currHealth);
		}

		if (currHealth <= 0)
		{
			GD.Print("Player died. Game over!");
			// scale: thêm vào các animation chết/ các banner/ etc
		}
	}

//hàm cơ bản của tiêu thụ consumables cho việc hồi phục, sẽ thay đổi khi có hết các vật phẩm có thể sử dụng được
	public void Consume(float hungerAmount, float sanityAmount = 0f, float healthAmount = 0f)
	{
		currHunger = Mathf.Min(maxHunger, currHunger + hungerAmount);

		if (MyHungerBar != null)
		{
			MyHungerBar.UpdateHunger(currHunger);
		}

		if (sanityAmount != 0f)
		{
			currSanity = Mathf.Clamp(currSanity + sanityAmount, 0f, maxSanity);
			if (MySanityBar != null)
			{
				MySanityBar.UpdateSanity(currSanity);
			}
		}

		if (healthAmount != 0f)
		{
			currHealth = Mathf.Clamp(currHealth + healthAmount, 0f, maxHealth);
			if (MyHealthBar != null)
			{
				MyHealthBar.UpdateHealth(currHealth);
			}
		}
		GD.Print($"Consumed. Current Health is: {currHealth} | Hunger: {currHunger} | Sanity: {currSanity}");
	}
}
