# res://scripts/save_load_menu.gd
extends CanvasLayer

enum Mode { SAVE, LOAD }

@export var mode: Mode = Mode.SAVE
@export var total_slots: int = 20

@onready var title_label: Label = %Title
@onready var slot_list: VBoxContainer = %SlotList
@onready var overlay: ColorRect = %Overlay

# Signal phát ra để game chính lắng nghe khi Load hoặc Save xong
signal save_requested(slot_index: int)
signal load_requested(save_data: Dictionary)
signal menu_closed

func _ready() -> void:
	# Cho phép click vào nền đen để đóng menu
	overlay.gui_input.connect(_on_overlay_gui_input)
	open_menu(mode)

func open_menu(p_mode: Mode) -> void:
	mode = p_mode
	self.show()
	
	if title_label:
		title_label.text = "SAVE GAME" if mode == Mode.SAVE else "LOAD GAME"
		
	refresh_slots()

func refresh_slots() -> void:
	# Xóa các slot mẫu cũ trong Editor
	for child in slot_list.get_children():
		child.queue_free()
		
	# Tạo danh sách slot động
	for i in range(1, total_slots + 1):
		var slot_container = _create_slot_ui(i)
		slot_list.add_child(slot_container)

func _create_slot_ui(slot_index: int) -> HBoxContainer:
	var hbox = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 8)
	
	# Nút bấm Slot
	var btn = Button.new()
	btn.custom_minimum_size = Vector2(0, 72)
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
	btn.text = SaveUtils.get_slot_info_text(slot_index)
	
	# Bắt sự kiện click
	btn.pressed.connect(func(): _on_slot_pressed(slot_index))
	hbox.add_child(btn)
	
	# Nếu có dữ liệu save -> Thêm nút Xóa (Delete)
	var data = SaveUtils.read_slot(slot_index)
	if not data.is_empty():
		var del_btn = Button.new()
		del_btn.custom_minimum_size = Vector2(48, 0)
		del_btn.text = "X"
		del_btn.pressed.connect(func(): _on_delete_pressed(slot_index))
		hbox.add_child(del_btn)
		
	return hbox

func _on_slot_pressed(slot_index: int) -> void:
	if mode == Mode.SAVE:
		# Ví dụ dữ liệu game của bạn cần lưu
		var current_game_data = {
			"player_hp": 100,
			"level_name": "Forest Area",
			"last_dialog": "Hero: Let's move on!"
		}
		
		# Phát signal để Game Manager truyền dữ liệu vào nếu cần
		save_requested.emit(slot_index)
		
		# Tiến hành ghi file
		if SaveUtils.write_slot(slot_index, current_game_data):
			print("Đã lưu thành công vào Slot: ", slot_index)
			refresh_slots() # Tải lại UI
			
	elif mode == Mode.LOAD:
		var data = SaveUtils.read_slot(slot_index)
		if not data.is_empty():
			print("Đang tải dữ liệu Slot: ", slot_index)
			load_requested.emit(data)
			close_menu()
		else:
			print("Slot rỗng, không thể load!")

func _on_delete_pressed(slot_index: int) -> void:
	SaveUtils.clear_slot(slot_index)
	refresh_slots()

func _on_overlay_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		close_menu()

func close_menu() -> void:
	self.hide()
	menu_closed.emit()