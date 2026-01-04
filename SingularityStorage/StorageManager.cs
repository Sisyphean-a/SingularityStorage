using StardewModdingAPI;
using StardewValley;
using SingularityStorage.Data;

namespace SingularityStorage
{
    /// <summary>
    /// 管理外部库存文件的加载、保存和访问。
    /// </summary>
    public static class StorageManager
    {
        private static IMonitor? _monitor;
        private static IDataHelper? _dataHelper;
        
        // 缓存：GUID -> InventoryData
        private static readonly Dictionary<string, SingularityInventoryData> LoadedInventories = new();

        public static void Initialize(IMonitor monitor, IDataHelper dataHelper)
        {
            _monitor = monitor;
            _dataHelper = dataHelper;
        }

        /// <summary>
        /// 获取特定 GUID 的库存数据。如果需要，从磁盘加载。
        /// </summary>
        public static SingularityInventoryData GetInventory(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                throw new ArgumentException("GUID cannot be null or empty.");

            if (LoadedInventories.TryGetValue(guid, out var data))
            {
                return data;
            }

            // 尝试从文件加载
            var filename = $"SingularityInventory_{guid}.json";
            data = _dataHelper!.ReadJsonFile<SingularityInventoryData>($"SaveData/{filename}") ?? new SingularityInventoryData(guid);
            
            LoadedInventories[guid] = data;
            return data;
        }

        /// <summary>
        /// 向存储中添加物品。
        /// </summary>
        /// <returns>如果物品已全部或部分添加则返回 true；如果由于容量不足而被拒绝则返回 false。</returns>
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

            // 1. 尝试与现有堆叠合并（不增加槽位占用）
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

            // 2. 如果仍有物品需要添加，我们需要一个新的槽位。
            if (item.Stack <= 0) return added;
            // 检查容量
            var currentUsedSlots = data.Inventory.Values.Sum(list => list.Count);
            if (currentUsedSlots >= data.MaxCapacity)
            {
                _monitor?.Log($"物品添加被拒绝：存储已满 ({currentUsedSlots}/{data.MaxCapacity})", LogLevel.Warn);
                return added;
            }

            _monitor?.Log($"添加新堆叠：{item.Name} x{item.Stack}", LogLevel.Trace);
                
            // 将物品复制到存储
            var storedItem = item.getOne();
            storedItem.Stack = item.Stack;
            stackList.Add(storedItem);
                
            // 标记输入物品已被完全消耗
            item.Stack = 0;
            added = true;

            return added;
        }

        /// <summary>
        /// 手动升级存储容量。
        /// </summary>
        public static void UpgradeCapacity(string guid, int increment)
        {
            var data = GetInventory(guid);
            data.MaxCapacity += increment;
            _monitor?.Log($"Upgraded storage {guid} capacity to {data.MaxCapacity} (+{increment})", LogLevel.Info);
        }

        /// <summary>
        /// 返回 (已用槽位, 最大容量)
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

            // 清理空列表
            if (stackList.Count == 0)
            {
                data.Inventory.Remove(key);
            }
        }

        /// <summary>
        /// 将所有加载的库存保存到磁盘。应在 GameLoop.Saving 时调用。
        /// </summary>
        public static void SaveAll()
        {
            if (_dataHelper == null) return;

            _monitor?.Log($"正在保存 {LoadedInventories.Count} 个外部库存...", LogLevel.Info);

            foreach (var kvp in LoadedInventories)
            {
                var filename = $"SingularityInventory_{kvp.Key}.json";
                _dataHelper.WriteJsonFile($"SaveData/{filename}", kvp.Value);
            }
        }
        
        /// <summary>
        /// 清除缓存。应在 SaveLoaded / ReturnToTitle 时调用。
        /// </summary>
        public static void ClearCache()
        {
            LoadedInventories.Clear();
        }
        
        /// <summary>
        /// 返回一个平铺的所有物品列表，用于 UI 显示。
        /// 此操作开销较大，将来应进行缓存或分页。
        /// </summary>
        public static IList<Item> GetAllItems(string guid)
        {
             var data = GetInventory(guid);
             return data.Inventory.Values.SelectMany(x => x).ToList();
        }
    }
}
