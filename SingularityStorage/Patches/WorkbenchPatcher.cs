using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace SingularityStorage.Patches
{
    /// <summary>
    /// 补丁 CraftingPage 以注入奇点箱子作为材料容器
    /// 使用反射来访问 materialContainers 字段，避免构造函数签名不匹配的问题
    /// </summary>
    [HarmonyPatch(typeof(CraftingPage))]
    public static class WorkbenchPatcher
    {
        private static FieldInfo _materialContainersField;

        static WorkbenchPatcher()
        {
            // 尝试获取 materialContainers 字段 (可能是 _materialContainers 或 materialContainers)
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            
            _materialContainersField = typeof(CraftingPage).GetField("_materialContainers", flags) 
                                       ?? typeof(CraftingPage).GetField("materialContainers", flags);
        }

        /// <summary>
        /// 动态查找目标构造函数
        /// </summary>
        /// <param name="harmony"></param>
        /// <returns></returns>
        public static MethodBase TargetMethod()
        {
            // 获取 CraftingPage 的所有构造函数
            var constructors = typeof(CraftingPage).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            // 我们寻找参数最多的那个构造函数，通常这就是我们要补丁的主构造函数
            // 如果有多个，这就提供了最好的兼容性
            return constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        }

        /// <summary>
        /// 后置补丁
        /// </summary>
        [HarmonyPostfix]
        public static void Constructor_Postfix(CraftingPage __instance)
        {
            InjectSingularityChests(__instance);
        }

        private static void InjectSingularityChests(CraftingPage instance)
        {
            try
            {
                if (_materialContainersField == null) return;

                var materialContainers = _materialContainersField.GetValue(instance) as List<Chest>;
                if (materialContainers == null) return;

                if (Game1.currentLocation == null) return;

                // 扫描当前位置的所有奇点箱子
                foreach (var obj in Game1.currentLocation.Objects.Values)
                {
                    if (obj.QualifiedItemId == "(BC)Singularity.Storage_SingularityChest" || 
                        obj.ItemId == "Singularity.Storage_SingularityChest")
                    {
                        if (obj.modData.TryGetValue("SingularityData_GUID", out string guid))
                        {
                            // 获取虚拟存储中的物品
                            var items = StorageManager.GetAllItems(guid);
                            
                            // 创建代理箱子
                            Chest proxy = new Chest(true);
                            
                            // 将物品添加到代理箱子（暂时注释，等待实现同步逻辑）
                            // proxy.Items.AddRange(items);
                            
                            // 添加到材料容器列表
                            materialContainers.Add(proxy);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默失败，避免崩溃
                ModEntry.Instance?.Monitor.Log($"Failed to inject Singularity Chests: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
            }
        }
    }
}
