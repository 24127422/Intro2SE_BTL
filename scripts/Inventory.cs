using Godot;
using System.Collections.Generic;

// LƯU Ý: Node này phải được đăng ký làm Autoload (Singleton) trong 
// Project Settings > Autoload, đặt tên là "Inventory"
public partial class Inventory : Node
{
	public static Inventory Instance { get; private set; }

	[Export] public int MaxSlots { get; set; } = 10;

	public List<InventorySlotData> Slots { get; private set; } = new();

	[Signal] public delegate void InventoryChangedEventHandler();
	[Signal] public delegate void ItemAddedEventHandler(Item item, int amount);
	[Signal] public delegate void ItemRemovedEventHandler(Item item, int amount);
	[Signal] public delegate void ItemDroppedEventHandler(Item item, int amount, float durability);
	[Signal] public delegate void ActiveSlotChangedEventHandler(int newIndex);

	private int _activeSlotIndex = 0;
	public int ActiveSlotIndex
	{
		get => _activeSlotIndex;
		set
		{
			int ClampedValue = Mathf.Clamp(value, 0, MaxSlots - 1);
			if(_activeSlotIndex != ClampedValue)
			{
				_activeSlotIndex = ClampedValue;
				EmitSignal(SignalName.ActiveSlotChanged, _activeSlotIndex);
			}
		}
	}

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
		Slots.Clear();

		for (int i = 0; i < MaxSlots; i++)
			Slots.Add(new InventorySlotData());
	}

	// Dùng khi bắt đầu game mới / Retry sau Game Over — dọn sạch túi đồ cũ.
	public void ResetForNewGame()
	{
		Slots.Clear();
		for (int i = 0; i < MaxSlots; i++)
			Slots.Add(new InventorySlotData());

		_activeSlotIndex = 0;
		EmitSignal(SignalName.InventoryChanged);
		EmitSignal(SignalName.ActiveSlotChanged, _activeSlotIndex);
	}

	public bool AddItem(Item item, int amount = 1, float? initialDurability = null)
	{
		if (item == null || amount <= 0) return false;

		int remaining = amount;

		if (item.MaxStackSize > 1)
		{
			foreach (var slot in Slots)
			{
				if (remaining <= 0) break;
				if (slot.Item == item && slot.Quantity < item.MaxStackSize)
				{
					int space = item.MaxStackSize - slot.Quantity;
					int addAmount = Mathf.Min(space, remaining);
					slot.Quantity += addAmount;
					remaining -= addAmount;
				}
			}
		}

		while (remaining > 0)
		{
			var emptySlot = Slots.Find(s => s.IsEmpty);
			if (emptySlot == null)
			{
				int actuallyAdded = amount - remaining;
				if (actuallyAdded > 0)
				{
					EmitSignal(SignalName.ItemAdded, item, actuallyAdded);
					EmitSignal(SignalName.InventoryChanged);
				}
				GD.Print("Túi đồ đã đầy!");
				return false;
			}

			int stackSize = item.MaxStackSize > 1 ? item.MaxStackSize : 1;
			int addAmount = Mathf.Min(stackSize, remaining);
			emptySlot.Item = item;
			emptySlot.Quantity = addAmount;

			if (item is PrimaryItem)
				emptySlot.CurrentDurability = initialDurability;

			remaining -= addAmount;
		}

		EmitSignal(SignalName.ItemAdded, item, amount);
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public bool RemoveItem(Item item, int amount = 1)
	{
		int remaining = amount;
		for (int i = Slots.Count - 1; i >= 0 && remaining > 0; i--)
		{
			var slot = Slots[i];
			if (slot.Item == item)
			{
				int removeAmount = Mathf.Min(slot.Quantity, remaining);
				slot.Quantity -= removeAmount;
				remaining -= removeAmount;
				if (slot.Quantity <= 0) slot.Clear();
			}
		}

		bool success = remaining <= 0;
		if (amount - remaining > 0)
		{
			EmitSignal(SignalName.ItemRemoved, item, amount - remaining);
			EmitSignal(SignalName.InventoryChanged);
		}
		return success;
	}

	public void RemoveAt(int index, int amount = 1)
	{
		if (index < 0 || index >= Slots.Count) return;
		var slot = Slots[index];
		if (slot.IsEmpty) return;

		var item = slot.Item;
		int removeAmount = Mathf.Min(slot.Quantity, amount);
		slot.Quantity -= removeAmount;
		if (slot.Quantity <= 0) slot.Clear();

		EmitSignal(SignalName.ItemRemoved, item, removeAmount);
		EmitSignal(SignalName.InventoryChanged);
	}

	public void DropItem(int index, int amount = 1)
	{
		if (index < 0 || index >= Slots.Count) return;
		var slot = Slots[index];
		if (slot.IsEmpty) return;

		var item = slot.Item;
		int dropAmount = Mathf.Min(slot.Quantity, amount);

		float durability = slot.CurrentDurability
			?? (item is PrimaryItem primary ? primary.MaxDurability : 0f);

		RemoveAt(index, dropAmount);
		EmitSignal(SignalName.ItemDropped, item, dropAmount, durability);
	}

	public void UseItem(int index)
	{
		if (index < 0 || index >= Slots.Count) return;
		var slot = Slots[index];
		if (slot.IsEmpty) return;

		if (slot.Item is PrimaryItem primary)
		{
			float current = slot.CurrentDurability ?? primary.MaxDurability;
			if (!slot.IsActive && primary.MaxDurability > 0f && current <= 0f)
			{
				GD.Print($"{primary.ItemName} đã hết năng lượng!");
				return;
			}

			bool nextActive = primary.IsToggleable ? !slot.IsActive : true;
			SetSlotActive(index, nextActive);
			return;
		}

		GD.Print($"Sử dụng: {slot.Item.ItemName}");

		if (slot.Item.IsConsumable)
		{
			PlayerStats.Instance?.Consume(
				hungerAmount: slot.Item.RestoreHunger,
				thirstAmount: slot.Item.RestoreThirst,
				sanityAmount: slot.Item.RestoreSanity,
				healthAmount: slot.Item.RestoreHealth
				);

			RemoveAt(index, 1);
		}
	}

	public bool SetSlotActive(int index, bool active)
	{
		if (index < 0 || index >= Slots.Count) return false;
		var slot = Slots[index];
		if (slot.IsEmpty || slot.Item is not PrimaryItem primary) return false;
		if (slot.IsActive == active) return true;

		slot.IsActive = active;
		slot.CurrentDurability ??= primary.MaxDurability;

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public void SwapSlots(int indexA, int indexB)
	{
		if (indexA < 0 || indexA >= Slots.Count) return;
		if (indexB < 0 || indexB >= Slots.Count) return;
		if (indexA == indexB) return;

		(Slots[indexA], Slots[indexB]) = (Slots[indexB], Slots[indexA]);
		EmitSignal(SignalName.InventoryChanged);
	}

	public bool HasItem(Item item, int amount = 1)
	{
		int total = 0;
		foreach (var slot in Slots)
			if (slot.Item == item) total += slot.Quantity;
		return total >= amount;
	}
}
