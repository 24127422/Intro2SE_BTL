using Godot;
using System;
using Godot.Collections;

public partial class Save_utils : Node
{
    private string save_directory = "user://saves/";

    public override void _Ready()
    {
        _EnsureSaveDirectory();
    }

    private void _EnsureSaveDirectory()
    {
        if (!DirAccess.DirExistsAbsolute(save_directory))
            DirAccess.MakeDirRecursiveAbsolute(save_directory);
    }

    public string GetSlotPath(int slotIndex)
    {
        return save_directory + "save_" + slotIndex.ToString("D2") + ".json";
    }

    public bool WriteSlot(int slotIndex, Dictionary data)
    {
        _EnsureSaveDirectory();
        data["save_time"] = Time.GetDatetimeStringFromSystem(false, true);
        var path = GetSlotPath(slotIndex);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            // Use Variant serialization to store the Dictionary
            file.StoreVar(data, true);
            file.Close();
            return true;
        }
        GD.Print("[SaveUtil] không ghi được file: ", path);
        return false;
    }

    public Dictionary ReadSlot(int slotIndex)
    {
        var path = GetSlotPath(slotIndex);
        if (!FileAccess.FileExists(path))
            return new Dictionary();
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            // Read as Variant that was stored via StoreVar
            var parsed = file.GetVar();
            file.Close();
            try
            {
                return (Godot.Collections.Dictionary)parsed;
            }
            catch
            {
                // fallback: not a dictionary
            }
        }
        return new Dictionary();
    }

    public void ClearSlot(int slotIndex)
    {
        var path = GetSlotPath(slotIndex);
        if (FileAccess.FileExists(path))
            DirAccess.RemoveAbsolute(path);
    }

    public string GetSlotInfoText(int slotIndex)
    {
        var data = ReadSlot(slotIndex);
        var slotStr = "[Slot " + slotIndex.ToString("D2") + "]";
        if (data.Count == 0)
            return slotStr + "(Empty Slot)";
        return slotStr + "\n";
    }
}
