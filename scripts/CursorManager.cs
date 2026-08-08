using Godot;
using System;

public partial class CursorManager : CanvasLayer
{
    private AnimatedSprite2D _sprite;
    private bool _isHovering = false;

    public override void _Ready()
    {
        // QUAN TRỌNG: Cho phép Node tiếp tục hoạt động khi Game bị Pause
        ProcessMode = ProcessModeEnum.Always;

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
                _sprite.Play(_isHovering ? "hover" : "idle");
            }
        }
    }

    public void SetHoverState(bool hovering)
    {
        _isHovering = hovering;
        
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _sprite.Play(_isHovering ? "hover" : "idle");
        }
    }
}