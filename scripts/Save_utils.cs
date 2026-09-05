using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class Save_utils : Node
{
    private string save_directory = "user://saves/";
    private SaveGameData _queuedLoadData;
    private string _queuedLoadScenePath = "";

    public override void _Ready()
    {
        _EnsureSaveDirectory();
        GetTree().SceneChanged += OnSceneChanged;
    }

    private void OnSceneChanged()
    {
        if (_queuedLoadData != null && GetTree() != null && GetTree().CurrentScene != null)
        {
            if (!string.IsNullOrWhiteSpace(_queuedLoadScenePath)
                && GetTree().CurrentScene.SceneFilePath != _queuedLoadScenePath)
            {
                return;
            }

            ApplySaveData(_queuedLoadData);
            _queuedLoadData = null;
            _queuedLoadScenePath = "";
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
            snapshot.Player.Stamina = playerStats.GetCurrentStamina();
            snapshot.Player.ActiveModifiers = playerStats.GetActiveModifierSnapshots();
        }

        var player = FindNode<movement>(scene);
        if (player != null)
        {
            snapshot.Player.X = player.GlobalPosition.X;
            snapshot.Player.Y = player.GlobalPosition.Y;
            snapshot.Player.FacingDirection = player.FacingDirection;
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

        if (scene != null)
        {
            foreach (var pickup in FindNodes<ItemPickup>(scene))
            {
                if (pickup.ItemData == null || string.IsNullOrWhiteSpace(pickup.ItemData.ResourcePath))
                    continue;

                snapshot.GroundItems.Add(new GroundItemSaveSnapshot
                {
                    ItemPath = pickup.ItemData.ResourcePath,
                    X = pickup.GlobalPosition.X,
                    Y = pickup.GlobalPosition.Y,
                    Durability = pickup.Durability
                });
            }

            foreach (var station in FindNodes<ComputerAssemblyStation>(scene))
            {
                snapshot.ComputerStations.Add(new ComputerAssemblySaveSnapshot
                {
                    NodePath = scene.GetPathTo(station).ToString(),
                    InsertedPartPaths = station.GetInsertedPartPaths(),
                    BodyComplete = station.BodyComplete,
                    IsAssembled = station.IsAssembled,
                    RomWasCorrect = station.RomWasCorrect
                });
            }
        }

        return snapshot;
    }

    public void QueueLoadData(SaveGameData data)
    {
        _queuedLoadData = data;
        _queuedLoadScenePath = data?.ScenePath ?? "";
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
            RestoreItemPickups(scene, data.GroundItems);
            RestoreComputerStations(scene, data.ComputerStations);
        }

        var player = FindNode<movement>(scene);
        if (player != null)
        {
            var playerData = data.Player ?? new PlayerSaveSnapshot();
            player.GlobalPosition = new Vector2(playerData.X, playerData.Y);
            player.SetFacingDirection(playerData.FacingDirection);
        }

        var playerStats = FindNode<PlayerStats>(scene);
        if (playerStats != null)
        {
            playerStats.ApplySnapshot(data.Player ?? new PlayerSaveSnapshot());
        }

        var inventory = Inventory.Instance;
        if (inventory != null)
        {
            inventory.Slots.Clear();
            var savedSlots = data.Inventory?.Slots;
            if (savedSlots != null)
            {
                for (int i = 0; i < savedSlots.Count; i++)
                {
                    var slotData = savedSlots[i];
                    var slot = new InventorySlotData();

                    if (slotData != null && !string.IsNullOrWhiteSpace(slotData.ItemPath))
                    {
                        var item = ResourceLoader.Load<Item>(slotData.ItemPath);
                        slot.Item = item;
                        slot.Quantity = Mathf.Max(0, slotData.Quantity);
                        slot.CurrentDurability = slotData.CurrentDurability;
                        slot.IsActive = slotData.IsActive;
                    }

                    inventory.Slots.Add(slot);
                }
            }

            while (inventory.Slots.Count < inventory.MaxSlots)
                inventory.Slots.Add(new InventorySlotData());

            int activeSlotIndex = data.Inventory?.ActiveSlotIndex ?? 0;
            inventory.ActiveSlotIndex = Mathf.Clamp(activeSlotIndex, 0, inventory.MaxSlots - 1);
            inventory.EmitSignal(Inventory.SignalName.InventoryChanged);
        }

        var journal = DocumentJournal.Instance;
        if (journal != null)
        {
            journal.RestoreUnlockedDocuments(data.Journal?.UnlockedDocumentPaths ?? new List<string>());
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

    private void RestoreItemPickups(Node scene, IEnumerable<GroundItemSaveSnapshot> savedItems)
    {
        if (scene == null || savedItems == null)
            return;

        var pickupScene = GD.Load<PackedScene>("res://tscn/item_pickup.tscn");
        if (pickupScene == null)
        {
            GD.PrintErr("[SaveUtil] Không tìm thấy scene item pickup để khôi phục item mặt đất.");
            return;
        }

        foreach (var savedItem in savedItems)
        {
            if (savedItem == null || string.IsNullOrWhiteSpace(savedItem.ItemPath))
                continue;

            var item = ResourceLoader.Load<Item>(savedItem.ItemPath);
            if (item == null)
            {
                GD.PrintErr("[SaveUtil] Không tải được item mặt đất: ", savedItem.ItemPath);
                continue;
            }

            var pickup = pickupScene.Instantiate<ItemPickup>();
            pickup.ItemData = item;
            pickup.Durability = savedItem.Durability;
            scene.AddChild(pickup);
            pickup.GlobalPosition = new Vector2(savedItem.X, savedItem.Y);
        }
    }

    private void RestoreComputerStations(Node scene, IEnumerable<ComputerAssemblySaveSnapshot> savedStations)
    {
        if (scene == null || savedStations == null)
            return;

        foreach (var savedStation in savedStations)
        {
            if (savedStation == null || string.IsNullOrWhiteSpace(savedStation.NodePath))
                continue;

            var station = scene.GetNodeOrNull<ComputerAssemblyStation>(savedStation.NodePath);
            station?.ApplySaveState(
                savedStation.InsertedPartPaths,
                savedStation.BodyComplete,
                savedStation.IsAssembled,
                savedStation.RomWasCorrect);
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

    private List<T> FindNodes<T>(Node root) where T : Node
    {
        var matches = new List<T>();
        if (root == null)
            return matches;

        var queue = new Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T match)
                matches.Add(match);

            foreach (Node child in current.GetChildren())
                queue.Enqueue(child);
        }

        return matches;
    }
}

