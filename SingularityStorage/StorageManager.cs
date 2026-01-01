using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static IMonitor? Monitor;
        private static IDataHelper? DataHelper;
        
        // Cache: GUID -> InventoryData
        private static readonly Dictionary<string, SingularityInventoryData> LoadedInventories = new();

        public static void Initialize(IMonitor monitor, IDataHelper dataHelper)
        {
            Monitor = monitor;
            DataHelper = dataHelper;
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
            string filename = $"SingularityInventory_{guid}.json";
            data = DataHelper!.ReadJsonFile<SingularityInventoryData>($"SaveData/{filename}") ?? new SingularityInventoryData(guid);
            
            LoadedInventories[guid] = data;
            return data;
        }

        /// <summary>
        /// Adds an item to the storage.
        /// </summary>
        public static void AddItem(string guid, Item item)
        {
            if (item == null) return;
            
            var data = GetInventory(guid);
            string key = item.QualifiedItemId;

            if (!data.Inventory.ContainsKey(key))
            {
                data.Inventory[key] = new List<Item>();
            }

            // Simple stack logic: try to merge with existing stacks
            // For now, we just add it. Advanced stacking logic comes later.
            // Actually, we should try to stack it to save RAM/Disk space.
            List<Item> stackList = data.Inventory[key];
            bool added = false;

            foreach (Item existing in stackList)
            {
                if (existing.canStackWith(item))
                {
                    int available = existing.maximumStackSize() - existing.Stack;
                    if (available > 0)
                    {
                        int toAdd = Math.Min(available, item.Stack);
                        existing.Stack += toAdd;
                        item.Stack -= toAdd;
                        if (item.Stack <= 0)
                        {
                            added = true;
                            break;
                        }
                    }
                }
            }

            if (!added && item.Stack > 0)
            {
                stackList.Add(item);
            }
        }

        /// <summary>
        /// Removes an item from the storage.
        /// </summary>
        public static void RemoveItem(string guid, Item item)
        {
            if (item == null) return;
            
            var data = GetInventory(guid);
            string key = item.QualifiedItemId;

            if (!data.Inventory.ContainsKey(key))
                return;

            List<Item> stackList = data.Inventory[key];
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
            if (DataHelper == null) return;

            Monitor?.Log($"Saving {LoadedInventories.Count} external inventories...", LogLevel.Info);

            foreach (var kvp in LoadedInventories)
            {
                string filename = $"SingularityInventory_{kvp.Key}.json";
                DataHelper.WriteJsonFile($"SaveData/{filename}", kvp.Value);
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
