using Godot;
using System;

public partial class CursorManager : CanvasLayer
{
    private AnimatedSprite2D _sprite;
    private bool _isHovering = false;

    public override void _Ready()
    {
        // Ẩn con trỏ mặc định của Windows/Mac
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        
        // Lấy tham chiếu đến Node AnimatedSprite2D
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _sprite.Play("idle");
    }

    public override void _Process(double delta)
    {
        // Luôn bám theo vị trí thực tế của chuột
        _sprite.GlobalPosition = _sprite.GetGlobalMousePosition();
    }

    public override void _Input(InputEvent @event)
    {
        // Xử lý khi click chuột trái (đổi sang press hoặc nhả ra)
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (mouseBtn.Pressed)
            {
                _sprite.Play("click");
            }
            else
            {
                // Khi nhả chuột, ưu tiên trở về "hover" nếu đang trỏ vào nút, ngược lại là "idle"
                _sprite.Play(_isHovering ? "hover" : "idle");
            }
        }
    }

    // Hàm này sẽ được các Nút bấm (Button) gọi đến khi rê chuột vào/ra
    public void SetHoverState(bool hovering)
    {
        _isHovering = hovering;
        
        // Chỉ đổi animation nếu người chơi không đang đè giữ chuột
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _sprite.Play(_isHovering ? "hover" : "idle");
        }
    }
}