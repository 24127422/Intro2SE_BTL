using Godot;
using System.Collections.Generic;

// LƯU Ý: Node này phải được đăng ký làm Autoload (Singleton) trong 
// Project Settings > Autoload, đặt tên là "Inventory"
public partial class Inventory : Node
{
	public static Inventory Instance { get; private set; }

	[Export] public int MaxSlots { get; set; } = 20;

	public List<InventorySlotData> Slots { get; private set; } = new();

	[Signal] public delegate void InventoryChangedEventHandler();
	[Signal] public delegate void ItemAddedEventHandler(Item item, int amount);
	[Signal] public delegate void ItemRemovedEventHandler(Item item, int amount);
	[Signal] public delegate void ItemDroppedEventHandler(Item item, int amount);
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

	// Thêm item vào túi. Tự động gộp stack trước, rồi mới chiếm ô trống.
	// Trả về true nếu thêm được TOÀN BỘ số lượng yêu cầu.
	public bool AddItem(Item item, int amount = 1)
	{
		if (item == null || amount <= 0) return false;

		int remaining = amount;

		// Bước 1: dồn vào các stack đã có sẵn item này (nếu stack được)
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

		// Bước 2: nhét phần còn lại vào các ô trống
		while (remaining > 0)
		{
			var emptySlot = Slots.Find(s => s.IsEmpty);
			if (emptySlot == null)
			{
				// Túi đầy, không còn ô trống
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
			remaining -= addAmount;
		}

		EmitSignal(SignalName.ItemAdded, item, amount);
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	// Xóa 1 loại item ra khỏi túi (bất kể ở ô nào), theo số lượng
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

	// Xóa item tại 1 vị trí cụ thể (dùng khi vứt đồ từ UI)
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

		RemoveAt(index, dropAmount); // xóa khỏi dữ liệu túi đồ (đã tự EmitSignal ItemRemoved + InventoryChanged)
		EmitSignal(SignalName.ItemDropped, item, dropAmount);
	}

	public void UseItem(int index)
	{
		if (index < 0 || index >= Slots.Count) return;
		var slot = Slots[index];
		if (slot.IsEmpty) return;

		GD.Print($"Sử dụng: {slot.Item.ItemName}");
		// TODO: gọi logic riêng của từng item ở đây (hồi máu, buff, mở khóa...)

		if (slot.Item.IsConsumable)
		{
			RemoveAt(index, 1);
		}
	}

	// Hoán đổi 2 ô cho nhau (dùng khi kéo-thả sắp xếp lại túi đồ)
	public void SwapSlots(int indexA, int indexB)
	{
		if (indexA < 0 || indexA >= Slots.Count) return;
		if (indexB < 0 || indexB >= Slots.Count) return;
		if (indexA == indexB) return;

		(Slots[indexA], Slots[indexB]) = (Slots[indexB], Slots[indexA]);
		EmitSignal(SignalName.InventoryChanged);
	}

	// Kiểm tra túi có đủ số lượng item này không (hữu ích cho crafting, quest...)
	public bool HasItem(Item item, int amount = 1)
	{
		int total = 0;
		foreach (var slot in Slots)
			if (slot.Item == item) total += slot.Quantity;
		return total >= amount;
	}
}
