using System;
using HarmonyLib;
using StardewValley;
using SingularityStorage;

namespace SingularityStorage.Patches
{
    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.getDescription))]
    public static class ObjectDescriptionPatch
    {
        public static void Postfix(StardewValley.Object __instance, ref string __result)
        {
            if (__instance == null) return;
            
            // Check if it's our chest
            if (__instance.QualifiedItemId == "(BC)Singularity.Storage_SingularityChest" || __instance.ItemId == "Singularity.Storage_SingularityChest")
            {
                // Get Capacity and Count
                string? guid = __instance.modData.ContainsKey("SingularityData_GUID") ? __instance.modData["SingularityData_GUID"] : null;
                
                int used = 0;
                if (!string.IsNullOrEmpty(guid))
                {
                   var data = StorageManager.GetInventory(guid);
                   // Sum of all stacks
                   foreach(var list in data.Inventory.Values)
                   {
                       used += list.Count; // This counts slots, not total stack quantity.
                   }
                }

                string max = __instance.modData.ContainsKey("MaxCapacity") ? __instance.modData["MaxCapacity"] : "36";
                
                __result += $"\n\nSingularity Storage\nCapacity: {used} / {max}";
            }
        }
    }
}
