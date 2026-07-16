using Godot;

public partial class ItemPickup : Area2D
{
	[Export] public Item ItemData { get; set; }
	
	private Label _promptLabel;
	private bool _isPlayerInRange = false;
	private Inventory _inventory;

	public override void _Ready()
	{
		_inventory = GetNodeOrNull<Inventory>("/root/Inventory");

		// Lấy Node Label ra và ẩn nó đi lúc đầu
		_promptLabel = GetNode<Label>("Label");
		if(_promptLabel != null)
		{
			_promptLabel.Visible = false;
		}

		// Kết nối sự kiện va chạm
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		if(ItemData == null)
		{
			GD.PrintErr($"[Cảnh báo] '{Name}' chưa được gán ItemData.");
		}

		if (ItemData != null && ItemData.Icon != null)
		{
			GetNode<Sprite2D>("Sprite2D").Texture = ItemData.Icon;
		}
		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null && ItemData.Icon != null)
        {
            sprite.Texture = ItemData.Icon;
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
			if (_promptLabel != null) _promptLabel.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_isPlayerInRange && Input.IsActionJustPressed("interact"))
        {
            if (ItemData == null) return;

           
            if (_inventory == null)
            {
                GD.PrintErr("[LỖI] Autoload 'Inventory' chưa được thiết lập trong Project Settings!");
                return;
            }

            if (ItemData.IsDocument)
            {
                var documentJournal = GetNodeOrNull("/root/DocumentJournal");
                if (documentJournal != null)
                {
                    documentJournal.Call("UnlockDocument", ItemData);
                }
                else
                {
                    GD.PrintErr("[LỖI] Tìm thấy item dạng Document nhưng Autoload 'DocumentJournal' chưa được thiết lập!");
                }
            }

            bool success = _inventory.AddItem(ItemData);
            if (success)
            {
                QueueFree();
            }
        }
	}
}
