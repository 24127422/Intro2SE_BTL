extends Node

var save_directory: String = "user://saves"

func _ready() -> void:
    _ensure_save_directory()
func _ensure_save_directory() -> void:
    if not DirAccess.dir_exists_absolute(save_directory):
        DirAccess.make_dir_recursive_absolute(save_directory)

func get_slot_path(slot_index: int) -> String:
    return save_directory + "save_" + str(slot_index).pad_zeros(2) + ".json"

func write_slot(slot_index: int, data: Dictionary) -> bool:
    _ensure_save_directory()
    data["save_time"] = Time.get_datetime_string_from_system(false, true)
    var path = get_slot_path(slot_index)
    var file = FileAccess.open(path, FileAccess.WRITE)
    if file:
        file.store_string(JSON.stringify(data, "\t"))
        file.close()
        return true
    print("[SaveUtil] không ghi được file: ", path)
    return false

func read_slot(slot_index: int) -> Dictionary:
    var path = get_slot_path(slot_index)
    if not FileAccess.file_exists(path):
        return {}
    var file = FileAccess.open(path, FileAccess.READ)
    if file:
        var content = file.get_as_text()
        file.close()
        var parsed = JSON.parse_string(content)
        if parsed is Dictionary:
            return parsed
    return {}

func clear_slot(slot_index: int) -> void:
    var path = get_slot_path(slot_index)
    if FileAccess.file_exists(path):
        DirAccess.remove_absolute(path) 

func get_slot_info_text(slot_index: int) -> String:
    var data = read_slot(slot_index)
    var slot_str = "[Slot " + str(slot_index).pad_zeros(2) + "]"
    if data.is_empty():
        return slot_str + "(Empty Slot)"
    return slot_str + "\n"