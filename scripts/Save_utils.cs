using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class Save_utils : Node
{
    private string save_directory = "user://saves/";
    private SaveGameData _queuedLoadData;

    public override void _Ready()
    {
        _EnsureSaveDirectory();
        GetTree().SceneChanged += OnSceneChanged;
    }

    private void OnSceneChanged()
    {
        if (_queuedLoadData != null && GetTree() != null && GetTree().CurrentScene != null)
        {
            ApplySaveData(_queuedLoadData);
            _queuedLoadData = null;
        }
    }

    private void _EnsureSaveDirectory()
    {
        if (!DirAccess.DirExistsAbsolute(save_directory))
            DirAccess.MakeDirRecursiveAbsolute(save_directory);
    }

    public string GetSlotPath(int slotIndex)
    {
        return save_directory + "save_" + slotIndex.ToString() + ".json";
    }

    public int GetNextAvailableSlotIndex()
    {
        for (int i = 1; i <= 20; i++)
        {
            var path = GetSlotPath(i);
            if (!FileAccess.FileExists(path))
                return i;
        }

        return 1;
    }

    public bool WriteSlot(int slotIndex, SaveGameData data)
    {
        _EnsureSaveDirectory();
        data.SaveTime = Time.GetDatetimeStringFromSystem(false, true);

        var path = GetSlotPath(slotIndex);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = false
            });

            file.StoreString(json);
            file.Close();
            GD.Print("[SaveUtil] Đã ghi JSON slot ", slotIndex, " -> ", path);
            return true;
        }

        GD.Print("[SaveUtil] không ghi được file: ", path);
        return false;
    }

    public SaveGameData ReadSlot(int slotIndex)
    {
        var path = GetSlotPath(slotIndex);
        if (!FileAccess.FileExists(path))
            return null;

        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            try
            {
                var json = file.GetAsText();
                file.Close();
                return JsonSerializer.Deserialize<SaveGameData>(json);
            }
            catch (Exception ex)
            {
                GD.PrintErr("[SaveUtil] JSON slot lỗi: ", ex.Message);
                return null;
            }
        }

        return null;
    }

    public SaveGameData LoadLatestSlot()
    {
        SaveGameData latest = null;
        string latestSaveTime = "";

        for (int i = 1; i <= 20; i++)
        {
            var data = ReadSlot(i);
            if (data == null || string.IsNullOrWhiteSpace(data.SaveTime))
                continue;

            if (latest == null || string.CompareOrdinal(data.SaveTime, latestSaveTime) > 0)
            {
                latest = data;
                latestSaveTime = data.SaveTime;
            }
        }

        return latest;
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
        var slotStr = "[save_" + slotIndex.ToString() + "]";
        if (data == null)
            return slotStr + " (Empty Slot)";

        if (string.IsNullOrWhiteSpace(data.SaveTime))
            return slotStr + "\n<no timestamp>";

        return slotStr + "\n" + data.SaveTime;
    }

    public SaveGameData CaptureCurrentGame()
    {
        var snapshot = new SaveGameData();
        snapshot.SaveTime = Time.GetDatetimeStringFromSystem(false, true);

        var scene = GetTree().CurrentScene;
        if (scene != null)
            snapshot.ScenePath = scene.SceneFilePath;

        var playerStats = FindNode<PlayerStats>(scene);
        if (playerStats != null)
        {
            snapshot.Player.Health = playerStats.GetCurrentHealth();
            snapshot.Player.Hunger = playerStats.GetCurrentHunger();
            snapshot.Player.Thirst = playerStats.GetCurrentThirst();
            snapshot.Player.Sanity = playerStats.GetCurrentSanity();
        }

        var player = FindNode<movement>(scene);
        if (player != null)
        {
            snapshot.Player.X = player.GlobalPosition.X;
            snapshot.Player.Y = player.GlobalPosition.Y;
        }

        var inventory = Inventory.Instance;
        if (inventory != null)
        {
            snapshot.Inventory.ActiveSlotIndex = inventory.ActiveSlotIndex;
            snapshot.Inventory.Slots.Clear();

            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                var slot = inventory.Slots[i];
                var item = slot.Item;

                snapshot.Inventory.Slots.Add(new InventorySlotSaveSnapshot
                {
                    ItemPath = item?.ResourcePath ?? "",
                    Quantity = slot.Quantity,
                    CurrentDurability = slot.CurrentDurability,
                    IsActive = slot.IsActive
                });
            }
        }

        var journal = DocumentJournal.Instance;
        if (journal != null)
        {
            snapshot.Journal.UnlockedDocumentPaths = journal.GetUnlockedDocumentPaths();
        }

        return snapshot;
    }

    public void QueueLoadData(SaveGameData data)
    {
        _queuedLoadData = data;
    }

    public SaveGameData GetQueuedLoadData() => _queuedLoadData;

    public void ApplySaveData(SaveGameData data)
    {
        if (data == null)
            return;

        var scene = GetTree().CurrentScene;
        if (scene != null)
        {
            ClearItemPickups(scene);
        }

        var player = FindNode<movement>(scene);
        if (player != null)
        {
            player.GlobalPosition = new Vector2(data.Player.X, data.Player.Y);
        }

        var playerStats = FindNode<PlayerStats>(scene);
        if (playerStats != null)
        {
            playerStats.ApplySnapshot(data.Player);
        }

        var inventory = Inventory.Instance;
        if (inventory != null)
        {
            inventory.Slots.Clear();
            for (int i = 0; i < data.Inventory.Slots.Count; i++)
            {
                var slotData = data.Inventory.Slots[i];
                var slot = new InventorySlotData();

                if (!string.IsNullOrWhiteSpace(slotData.ItemPath))
                {
                    var item = ResourceLoader.Load<Item>(slotData.ItemPath);
                    slot.Item = item;
                    slot.Quantity = Mathf.Max(0, slotData.Quantity);
                    slot.CurrentDurability = slotData.CurrentDurability;
                    slot.IsActive = slotData.IsActive;
                }

                inventory.Slots.Add(slot);
            }

            inventory.ActiveSlotIndex = Mathf.Clamp(data.Inventory.ActiveSlotIndex, 0, inventory.Slots.Count - 1);
            inventory.EmitSignal(Inventory.SignalName.InventoryChanged);
        }

        var journal = DocumentJournal.Instance;
        if (journal != null)
        {
            journal.RestoreUnlockedDocuments(data.Journal.UnlockedDocumentPaths);
        }
    }

    private void ClearItemPickups(Node root)
    {
        if (root == null)
            return;

        var queue = new Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (Node child in current.GetChildren())
            {
                queue.Enqueue(child);
            }

            if (current is ItemPickup pickup)
            {
                pickup.QueueFree();
            }
        }
    }

    private T FindNode<T>(Node root) where T : Node
    {
        if (root == null)
            return null;

        var queue = new Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T match)
                return match;

            foreach (Node child in current.GetChildren())
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }
}

