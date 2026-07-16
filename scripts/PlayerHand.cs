using Godot;

public partial class PlayerHand : Node2D
{
    [Export] public Node2D HandMarker { get; set; }
    private Node2D _currentHeldNode = null;
    private Item _currentActiveItem = null; 
    private const int HotbarSize = 9;
    private movement _playerMovement;

    
    [ExportGroup("Tọa độ tay (Offsets)")]
    [Export] public Vector2 OffsetNorth { get; set; } = new Vector2(0, -14); 
    [Export] public Vector2 OffsetSouth { get; set; } = new Vector2(0, 12);   
    [Export] public Vector2 OffsetEast { get; set; } = new Vector2(14, 4);   
    [Export] public Vector2 OffsetWest { get; set; } = new Vector2(-14, 4);  

    
    [ExportGroup("Góc xoay tay (Rotations trong độ)")]
    [Export] public float RotationNorth { get; set; } = -45f; 
    [Export] public float RotationSouth { get; set; } = 45f;  
    [Export] public float RotationEast { get; set; } = 0f;    
    [Export] public float RotationWest { get; set; } = 0f;    

    
    [ExportGroup("Thứ tự đè (Z Index)")]
    [Export] public int ZIndexNorth { get; set; } = -1; 
    [Export] public int ZIndexSouth { get; set; } = 1;  
    [Export] public int ZIndexEast { get; set; } = 1;   
    [Export] public int ZIndexWest { get; set; } = 1;   

    public override void _Ready()
    {
        if (HandMarker == null)
        {
            HandMarker = this;
        }

        
        _playerMovement = FindPlayerMovement();

        if (_playerMovement != null)
        {
            _playerMovement.Connect("FacingDirectionChanged", new Callable(this, nameof(OnFacingDirectionChanged)));
            GD.Print($"[PlayerHand] Đã kết nối thành công với Player: {_playerMovement.Name}");
        }
        else
        {
            GD.PrintErr("[PlayerHand] LỖI: Không tìm thấy script 'movement' trên bất kỳ Node cha nào!");
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.InventoryChanged += UpdateHeldItem;
            Inventory.Instance.ActiveSlotChanged += OnActiveSlotChanged;
        }
        UpdateHeldItem();
    }

    
    private movement FindPlayerMovement()
    {
        Node current = this;
        while (current != null)
        {
            if (current is movement playerMove)
            {
                return playerMove;
            }
            current = current.GetParent();
        }
        return null;
    }

    public override void _ExitTree()
    {
        if (_playerMovement != null)
        {
            _playerMovement.Disconnect("FacingDirectionChanged", new Callable(this, nameof(OnFacingDirectionChanged)));
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.InventoryChanged -= UpdateHeldItem;
            Inventory.Instance.ActiveSlotChanged -= OnActiveSlotChanged;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
            {
                int targetSlot = (int)keyEvent.Keycode - (int)Key.Key1;
                Inventory.Instance.ActiveSlotIndex = targetSlot;
            }
        }

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
            {
                int prevSlot = (Inventory.Instance.ActiveSlotIndex - 1 + HotbarSize) % HotbarSize;
                Inventory.Instance.ActiveSlotIndex = prevSlot;
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown)
            {
                int nextSlot = (Inventory.Instance.ActiveSlotIndex + 1) % HotbarSize;
                Inventory.Instance.ActiveSlotIndex = nextSlot;
            }
        }
    }

    private void UpdateHeldItem()
    {
        
        if (_currentHeldNode != null && GodotObject.IsInstanceValid(_currentHeldNode))
        {
            _currentHeldNode.QueueFree();
            _currentHeldNode = null;
        }
        _currentActiveItem = null;

        if (Inventory.Instance == null || HandMarker == null) return;

        int activeIndex = Inventory.Instance.ActiveSlotIndex;
        if (activeIndex < 0 || activeIndex >= Inventory.Instance.Slots.Count) return;

        var activeSlot = Inventory.Instance.Slots[activeIndex];

        if (activeSlot == null || activeSlot.IsEmpty || activeSlot.Item == null)
        {
            return;
        }

        Item item = activeSlot.Item;
        _currentActiveItem = item;

        
        if (item.HandModel != null)
        {
            Node2D modelInstance = item.HandModel.Instantiate<Node2D>();
            HandMarker.AddChild(modelInstance);
            modelInstance.Position = Vector2.Zero;
            modelInstance.Rotation = 0f;
            _currentHeldNode = modelInstance;
        }
        else if (item.Icon != null)
        {
            Sprite2D fallbackSprite = new Sprite2D();
            fallbackSprite.Texture = item.Icon;
            HandMarker.AddChild(fallbackSprite);
            fallbackSprite.Position = Vector2.Zero;
            fallbackSprite.Rotation = 0f;
            _currentHeldNode = fallbackSprite;
        }

        ApplyFacingDirection(_playerMovement?.FacingDirection ?? "S");
    }

    private void OnFacingDirectionChanged(string newDirection)
    {
        ApplyFacingDirection(newDirection);
    }

    private void ApplyFacingDirection(string direction)
    {
        
        Vector2 targetOffset = OffsetSouth;
        float targetRotationDegrees = RotationSouth;
        int targetZIndex = ZIndexSouth;

        switch (direction)
        {
            case "N":
                targetOffset = OffsetNorth;
                targetRotationDegrees = RotationNorth;
                targetZIndex = ZIndexNorth;
                break;
            case "S":
                targetOffset = OffsetSouth;
                targetRotationDegrees = RotationSouth;
                targetZIndex = ZIndexSouth;
                break;
            case "E":
                targetOffset = OffsetEast;
                targetRotationDegrees = RotationEast;
                targetZIndex = ZIndexEast;
                break;
            case "W":
                targetOffset = OffsetWest;
                targetRotationDegrees = RotationWest;
                targetZIndex = ZIndexWest;
                break;
        }

        Node2D targetNode = HandMarker ?? this;
        targetNode.Position = targetOffset;
        targetNode.Rotation = Mathf.DegToRad(targetRotationDegrees);
        targetNode.ZIndex = targetZIndex;
        targetNode.Scale = Vector2.One; 

        if (_currentHeldNode == null || _currentActiveItem == null) return;

        bool hasDirectionalTextures = _currentActiveItem.TextureNorth != null || 
                                     _currentActiveItem.TextureSouth != null || 
                                     _currentActiveItem.TextureEast != null || 
                                     _currentActiveItem.TextureWest != null;

        if (_currentHeldNode is Sprite2D sprite)
        {
            sprite.Scale = Vector2.One; 

            if (hasDirectionalTextures)
            {
                switch (direction)
                {
                    case "N":
                        if (_currentActiveItem.TextureNorth != null) sprite.Texture = _currentActiveItem.TextureNorth;
                        break;
                    case "S":
                        if (_currentActiveItem.TextureSouth != null) sprite.Texture = _currentActiveItem.TextureSouth;
                        break;
                    case "E":
                        if (_currentActiveItem.TextureEast != null) sprite.Texture = _currentActiveItem.TextureEast;
                        break;
                    case "W":
                        if (_currentActiveItem.TextureWest != null)
                        {
                            sprite.Texture = _currentActiveItem.TextureWest;
                        }
                        else if (_currentActiveItem.TextureEast != null)
                        {
                            
                            sprite.Texture = _currentActiveItem.TextureEast;
                            sprite.Scale = new Vector2(-1f, 1f); 
                        }
                        break;
                }
            }
            else
            {
                
                sprite.Texture = _currentActiveItem.Icon;
                bool facingLeft = direction == "W";
                sprite.Scale = new Vector2(facingLeft ? -1f : 1f, 1f);
            }
        }
    }

    private void OnActiveSlotChanged(int newIndex)
    {
        UpdateHeldItem();
    }
}