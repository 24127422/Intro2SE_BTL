using Godot;

public partial class ItemPickup : Area2D
{
	[Export] public Item ItemData { get; set; }
	
	private Label _promptLabel;
	private bool _isPlayerInRange = false;
	private Inventory _playerInventory;

	public override void _Ready()
	{
		// Lấy Node Label ra và ẩn nó đi lúc đầu
		_promptLabel = GetNode<Label>("Label");
		_promptLabel.Visible = false;

		// Kết nối sự kiện va chạm
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		if (ItemData != null && ItemData.Icon != null)
		{
			GetNode<Sprite2D>("Sprite2D").Texture = ItemData.Icon;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		// Kiểm tra nếu vật thể bước vào vùng là Nhân vật
		if (body is CharacterBody2D)
		{
			_isPlayerInRange = true;
			_promptLabel.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		// Khi nhân vật đi ra xa khỏi vật phẩm
		if (body is CharacterBody2D)
		{
			_isPlayerInRange = false;
			_promptLabel.Visible = false; // Ẩn chữ đi
			_playerInventory = null;
		}
	}

	public override void _Process(double delta)
	{
		if (_isPlayerInRange && Input.IsActionJustPressed("interact"))
		{
			if (ItemData != null)
			{
				if (ItemData.IsDocument)
				{
					DocumentJournal.Instance.UnlockDocument(ItemData);
					QueueFree();
				}
				else
				{
					bool success = Inventory.Instance.AddItem(ItemData);
					if (success)
					{
						QueueFree();
					}
				}
			}
		}
	}
}
