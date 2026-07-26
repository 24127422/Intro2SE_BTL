extends CanvasLayer

# --- KHAI BÁO CÁC NODE CON ---
@onready var save_button: Button = $UIContainer/MainPanel/Margin/VBoxContainer/SaveButton # Sửa đường dẫn theo đúng tree của bạn
@onready var load_button: Button = $UIContainer/MainPanel/Margin/VBoxContainer/LoadButton
@onready var settings_button: Button = $UIContainer/MainPanel/Margin/VBoxContainer/SettingsButton

# Reference tới Scene SaveLoadMenu (nếu nằm cùng Scene hoặc là Node con)
@export var save_load_menu: CanvasLayer
@export var settings_menu: Control

func _ready() -> void:
	# Mặc định ẩn Pause Menu khi mới bắt đầu Game
	self.hide()
	
	# Kết nối sự kiện bấm nút
	if save_button:
		save_button.pressed.connect(_on_save_pressed)
	if load_button:
		load_button.pressed.connect(_on_load_pressed)
	if settings_button:
		settings_button.pressed.connect(_on_settings_pressed)

	# Lắng nghe Signal thay đổi trạng thái Game từ GameManager (C# Autoload)
	if Engine.has_singleton("GameManager") or ClassDB.class_exists("GameManager"):
		# Cách kết nối Signal C# từ GDScript trong Godot 4
		var gm = get_node_or_null("/root/GameManager")
		if gm:
			gm.connect("GameStateChanged", _on_game_state_changed)

func _unhandled_input(event: InputEvent) -> void:
	# Bắt sự kiện phím ESC (sử dụng ui_cancel mặc định của Godot hoặc toggle_pause)
	if event.is_action_pressed("ui_cancel") or event.is_action_pressed("toggle_pause"):
		var gm = get_node_or_null("/root/GameManager")
		if gm:
			# Không cho bật Pause nếu đang ở MainMenu, Victory hoặc Defeat
			var current_state = gm.CurrentState
			if current_state == 0 or current_state == 3 or current_state == 4: # MainMenu=0, Victory=3, Defeat=4
				return
				
			# Chuyển đổi Pause/Unpause
			gm.TogglePause()
			get_viewport().set_input_as_handled()

# Lắng nghe trạng thái Pause từ GameManager để Ẩn/Hiện Menu
func _on_game_state_changed(new_state: int) -> void:
	if new_state == 2: # GameState.Paused = 2
		self.show()
	else:
		self.hide()
		if save_load_menu: save_load_menu.hide()
		if settings_menu: settings_menu.hide()

# --- XỬ LÝ SỰ KIỆN NÚT BẤM ---

func _on_save_pressed() -> void:
	if save_load_menu:
		# Gọi hàm open_menu trong GDScript của SaveLoadMenu (0 = Mode.SAVE)
		save_load_menu.open_menu(0)

func _on_load_pressed() -> void:
	if save_load_menu:
		# Gọi hàm open_menu trong GDScript của SaveLoadMenu (1 = Mode.LOAD)
		save_load_menu.open_menu(1)

func _on_settings_pressed() -> void:
	if settings_menu:
		settings_menu.show()
	else:
		print("Chưa gán Settings Menu!")