extends CanvasLayer

@onready var resume_button = $MenuPanel/VBoxContainer/ResumeBtn
@onready var save_button = $MenuPanel/VBoxContainer/SaveBtn
@onready var quit_button = $MenuPanel/VBoxContainer/QuitBtn

func _ready() -> void:
    pause_mode = Node.PAUSE_MODE_PROCESS
    set_process_unhandled_input(true)
    hide()

    if resume_button:
        resume_button.pressed.connect(_on_resume_pressed)
    if save_button:
        save_button.pressed.connect(_on_save_pressed)
    if quit_button:
        quit_button.pressed.connect(_on_quit_pressed)

    var gm = get_node_or_null("/root/GameManager")
    if gm:
        gm.connect("GameStateChanged", self, "_on_game_state_changed")

func _unhandled_input(event: InputEvent) -> void:
    if event is InputEventKey and event.pressed and event.keycode == Key.ESCAPE:
        _toggle_pause()
    elif event.is_action_pressed("ui_cancel") or event.is_action_pressed("toggle_pause"):
        _toggle_pause()

func _toggle_pause() -> void:
    var gm = get_node_or_null("/root/GameManager")
    if not gm:
        return

    var current_state = gm.get("CurrentState")
    if current_state == 0 or current_state == 3 or current_state == 4:
        return

    gm.call("TogglePause")
    get_viewport().set_input_as_handled()

func _on_game_state_changed(new_state: int) -> void:
    if new_state == 2:
        show()
    else:
        hide()

func _on_resume_pressed() -> void:
    _toggle_pause()

func _on_save_pressed() -> void:
    # implement save logic if needed, or leave this button disabled
    pass

func _on_quit_pressed() -> void:
    get_tree().quit()
