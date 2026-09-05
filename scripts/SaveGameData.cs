using System.Collections.Generic;

public sealed class SaveGameData
{
    public string FormatVersion { get; set; } = "1.0";
    public string SaveTime { get; set; } = "";
    public string ScenePath { get; set; } = "";

    public PlayerSaveSnapshot Player { get; set; } = new PlayerSaveSnapshot();
    public InventorySaveSnapshot Inventory { get; set; } = new InventorySaveSnapshot();
    public JournalSaveSnapshot Journal { get; set; } = new JournalSaveSnapshot();
    public List<GroundItemSaveSnapshot> GroundItems { get; set; } = new List<GroundItemSaveSnapshot>();
    public List<ComputerAssemblySaveSnapshot> ComputerStations { get; set; } = new List<ComputerAssemblySaveSnapshot>();
}

public sealed class PlayerSaveSnapshot
{
    public float Health { get; set; } = 100f;
    public float Hunger { get; set; } = 100f;
    public float Thirst { get; set; } = 100f;
    public float Sanity { get; set; } = 100f;
    public float Stamina { get; set; } = 10f;
    public float X { get; set; } = 0f;
    public float Y { get; set; } = 0f;
    public string FacingDirection { get; set; } = "S";
    public List<StatModifierSaveSnapshot> ActiveModifiers { get; set; } = new List<StatModifierSaveSnapshot>();
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

public sealed class GroundItemSaveSnapshot
{
    public string ItemPath { get; set; } = "";
    public float X { get; set; } = 0f;
    public float Y { get; set; } = 0f;
    public float? Durability { get; set; } = null;
}

public sealed class ComputerAssemblySaveSnapshot
{
    public string NodePath { get; set; } = "";
    public List<string> InsertedPartPaths { get; set; } = new List<string>();
    public bool BodyComplete { get; set; } = false;
    public bool IsAssembled { get; set; } = false;
    public bool RomWasCorrect { get; set; } = false;
}

public sealed class StatModifierSaveSnapshot
{
    public string Source { get; set; } = "";
    public int Target { get; set; } = 0;
    public float RateMultiplier { get; set; } = 1f;
    public float FlatBonus { get; set; } = 0f;
    public float Duration { get; set; } = -1f;
    public float TimeRemaining { get; set; } = 0f;
}
