using HarmonyLib;

namespace SingularityStorage.Patches
{
    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.getDescription))]
    public static class ObjectDescriptionPatch
    {
        public static void Postfix(StardewValley.Object __instance, ref string __result)
        {
            if (__instance == null) return;
            
            // 检查是否为我们的箱子 (Singularity Chest)
            if (__instance.QualifiedItemId == "(BC)Singularity.Storage_SingularityChest" || __instance.ItemId == "Singularity.Storage_SingularityChest")
            {
                // 获取容量 (Capacity) 和已用数量 (Count)
                var guid = __instance.modData.ContainsKey("SingularityData_GUID") ? __instance.modData["SingularityData_GUID"] : null;
                
                var used = 0;
                if (!string.IsNullOrEmpty(guid))
                {
                   var data = StorageManager.GetInventory(guid);
                   // 所有物品堆叠的总和
                   foreach(var list in data.Inventory.Values)
                   {
                       used += list.Count; // 这里计算的是槽位数量，而不是总物品堆叠数量。
                   }
                }

                var max = __instance.modData.ContainsKey("MaxCapacity") ? __instance.modData["MaxCapacity"] : "36";
                
                __result += $"\n\n奇点存储\n容量: {used} / {max}";
            }
        }
    }
}
