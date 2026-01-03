using StardewModdingAPI;
using StardewValley;
using SingularityStorage.Data;

namespace SingularityStorage
{
    /// <summary>
    /// Manages the loading, saving, and access to external inventory files.
    /// </summary>
    public static class StorageManager
    {
        private static IMonitor? _monitor;
        private static IDataHelper? _dataHelper;
        
        // Cache: GUID -> InventoryData
        private static readonly Dictionary<string, SingularityInventoryData> LoadedInventories = new();

        public static void Initialize(IMonitor monitor, IDataHelper dataHelper)
        {
            _monitor = monitor;
            _dataHelper = dataHelper;
        }

        /// <summary>
        /// Gets the inventory data for a specific GUID. Loads from disk if necessary.
        /// </summary>
        public static SingularityInventoryData GetInventory(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                throw new ArgumentException("GUID cannot be null or empty.");

            if (LoadedInventories.TryGetValue(guid, out var data))
            {
                return data;
            }

            // Try load from file
            var filename = $"SingularityInventory_{guid}.json";
            data = _dataHelper!.ReadJsonFile<SingularityInventoryData>($"SaveData/{filename}") ?? new SingularityInventoryData(guid);
            
            LoadedInventories[guid] = data;
            return data;
        }

        /// <summary>
        /// Adds an item to the storage.
        /// </summary>
        /// <returns>True if the item was fully or partially added; false if rejected due to capacity.</returns>
        public static bool AddItem(string guid, Item? item)
        {
            if (item == null || item.Stack <= 0) return false;
            
            var data = GetInventory(guid);
            var key = item.QualifiedItemId;

            if (!data.Inventory.ContainsKey(key))
            {
                data.Inventory[key] = new List<Item>();
            }

            var stackList = data.Inventory[key];
            var added = false;

            // 1. Try to merge with existing stacks (doesn't increase slot usage)
            foreach (var existing in stackList)
            {
                if (!existing.canStackWith(item)) continue;
                var available = existing.maximumStackSize() - existing.Stack;
                if (available <= 0) continue;
                var toAdd = Math.Min(available, item.Stack);
                existing.Stack += toAdd;
                item.Stack -= toAdd;
                added = true;
                if (item.Stack <= 0)
                {
                    break;
                }
            }

            // 2. If we still have items to add, we need a new slot.
            if (item.Stack <= 0) return added;
            // Check Capacity
            var currentUsedSlots = data.Inventory.Values.Sum(list => list.Count);
            if (currentUsedSlots >= data.MaxCapacity)
            {
                _monitor?.Log($"Add Item Rejected: Storage Full ({currentUsedSlots}/{data.MaxCapacity})", LogLevel.Warn);
                return added;
            }

            _monitor?.Log($"Adding new stack: {item.Name} x{item.Stack}", LogLevel.Trace);
                
            // Copy the item to storage
            var storedItem = item.getOne();
            storedItem.Stack = item.Stack;
            stackList.Add(storedItem);
                
            // Mark input item as fully consumed
            item.Stack = 0;
            added = true;

            return added;
        }

        /// <summary>
        /// Manually upgrades the capacity of the storage.
        /// </summary>
        public static void UpgradeCapacity(string guid, int increment)
        {
            var data = GetInventory(guid);
            data.MaxCapacity += increment;
            _monitor?.Log($"Upgraded storage {guid} capacity to {data.MaxCapacity} (+{increment})", LogLevel.Info);
        }

        /// <summary>
        /// Returns (UsedSlots, MaxCapacity)
        /// </summary>
        public static (int Used, int Max) GetCounts(string guid)
        {
            var data = GetInventory(guid);
            var used = data.Inventory.Values.Sum(list => list.Count);
            return (used, data.MaxCapacity);
        }

        /// <summary>
        /// Removes an item from the storage.
        /// </summary>
        public static void RemoveItem(string guid, Item? item)
        {
            if (item == null) return;
            
            var data = GetInventory(guid);
            var key = item.QualifiedItemId;

            if (!data.Inventory.ContainsKey(key))
                return;

            var stackList = data.Inventory[key];
            stackList.Remove(item);

            // Clean up empty lists
            if (stackList.Count == 0)
            {
                data.Inventory.Remove(key);
            }
        }

        /// <summary>
        /// Saves all loaded inventories to disk. Should be called on GameLoop.Saving.
        /// </summary>
        public static void SaveAll()
        {
            if (_dataHelper == null) return;

            _monitor?.Log($"Saving {LoadedInventories.Count} external inventories...", LogLevel.Info);

            foreach (var kvp in LoadedInventories)
            {
                var filename = $"SingularityInventory_{kvp.Key}.json";
                _dataHelper.WriteJsonFile($"SaveData/{filename}", kvp.Value);
            }
        }
        
        /// <summary>
        /// Clears cache. Should be called on SaveLoaded / ReturnToTitle.
        /// </summary>
        public static void ClearCache()
        {
            LoadedInventories.Clear();
        }
        
        /// <summary>
        /// Returns a flattened list of all items for UI display.
        /// This is expensive and should be cached or paginated in the future.
        /// </summary>
        public static IList<Item> GetAllItems(string guid)
        {
             var data = GetInventory(guid);
             return data.Inventory.Values.SelectMany(x => x).ToList();
        }
    }
}
