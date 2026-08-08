using System.Collections.Generic;

public sealed class SaveGameData
{
    public string FormatVersion { get; set; } = "1.0";
    public string SaveTime { get; set; } = "";
    public string ScenePath { get; set; } = "";

    public PlayerSaveSnapshot Player { get; set; } = new PlayerSaveSnapshot();
    public InventorySaveSnapshot Inventory { get; set; } = new InventorySaveSnapshot();
    public JournalSaveSnapshot Journal { get; set; } = new JournalSaveSnapshot();
}

public sealed class PlayerSaveSnapshot
{
    public float Health { get; set; } = 100f;
    public float Hunger { get; set; } = 100f;
    public float Thirst { get; set; } = 100f;
    public float Sanity { get; set; } = 100f;
    public float X { get; set; } = 0f;
    public float Y { get; set; } = 0f;
}

public sealed class InventorySaveSnapshot
{
    public int ActiveSlotIndex { get; set; } = 0;
    public List<InventorySlotSaveSnapshot> Slots { get; set; } = new List<InventorySlotSaveSnapshot>();
}

public sealed class InventorySlotSaveSnapshot
{
    public string ItemPath { get; set; } = "";
    public int Quantity { get; set; } = 0;
    public float? CurrentDurability { get; set; } = null;
    public bool IsActive { get; set; } = false;
}

public sealed class JournalSaveSnapshot
{
    public List<string> UnlockedDocumentPaths { get; set; } = new List<string>();
}
