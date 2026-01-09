using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace SingularityStorage.Framework.Content
{
    /// <summary>
    /// 集中定义 Mod 中的所有物品和配方数据。
    /// </summary>
    public static class ItemDefinitions
    {
        public const string ModId = "Singularity.Storage"; // 以后可以从 Manifest 获取
        
        // Item IDs
        public const string SingularityChestId = $"{ModId}_SingularityChest";
        public const string UpgradeT1Id = $"{ModId}_T1_Comp";
        public const string UpgradeT2Id = $"{ModId}_T2_Comp";
        public const string UpgradeT3Id = $"{ModId}_T3_Comp";

        // Texture Keys (Assets)
        public const string TextureChest = "SingularityChest";
        public const string TextureUpgradeT1 = "Upgrade_T1";
        public const string TextureUpgradeT2 = "Upgrade_T2";
        public const string TextureUpgradeT3 = "Upgrade_T3";

        /// <summary>
        /// 获取所有 BigCraftable (机器/家具) 的定义。
        /// </summary>
        public static Dictionary<string, BigCraftableData> GetBigCraftables()
        {
            return new Dictionary<string, BigCraftableData>
            {
                [SingularityChestId] = new BigCraftableData
                {
                    Name = "SingularityChest", 
                    DisplayName = "奇点仓库", 
                    Price = 1000,
                    Description = "拥有虚拟化存储空间的箱子。",
                    Texture = $"Mods/{ModId}/{TextureChest}",
                    SpriteIndex = 0,
                    Fragility = 0,
                    CanBePlacedOutdoors = true
                }
            };
        }

        /// <summary>
        /// 获取所有普通 Object (升级组件等) 的定义。
        /// </summary>
        public static Dictionary<string, ObjectData> GetObjects()
        {
            return new Dictionary<string, ObjectData>
            {
                [UpgradeT1Id] = new ObjectData
                {
                    Name = UpgradeT1Id,
                    DisplayName = "基础存储升级",
                    Description = "为奇点仓库增加 36 个槽位。",
                    Type = "Basic",
                    Category = -16, // Crafting Category
                    Price = 1000,
                    Texture = $"Mods/{ModId}/{TextureUpgradeT1}",
                    SpriteIndex = 0
                },
                [UpgradeT2Id] = new ObjectData
                {
                    Name = UpgradeT2Id,
                    DisplayName = "高级存储升级",
                    Description = "为奇点仓库增加 100 个槽位。",
                    Type = "Basic",
                    Category = -16,
                    Price = 5000,
                    Texture = $"Mods/{ModId}/{TextureUpgradeT2}",
                    SpriteIndex = 0
                },
                [UpgradeT3Id] = new ObjectData
                {
                    Name = UpgradeT3Id,
                    DisplayName = "量子存储升级",
                    Description = "为奇点仓库增加 999 个槽位。",
                    Type = "Basic",
                    Category = -16,
                    Price = 20000,
                    Texture = $"Mods/{ModId}/{TextureUpgradeT3}",
                    SpriteIndex = 0
                }
            };
        }

        /// <summary>
        /// 获取所有制作配方的定义。
        /// </summary>
        public static List<RecipeModel> GetRecipes()
        {
            return new List<RecipeModel>
            {
                // 奇点仓库: 铱锭(337)x10, 金锭(336)x5, 电池组(787)x1
                new RecipeModel("Singularity Chest", SingularityChestId, 1, isBigCraftable: true)
                    .AddIngredient("337", 10)
                    .AddIngredient("336", 5)
                    .AddIngredient("787", 1),

                // 基础升级: 精炼石英(338)x5, 铁锭(335)x5
                new RecipeModel("Basic Storage Upgrade", UpgradeT1Id, 1)
                    .AddIngredient("338", 5)
                    .AddIngredient("335", 5),

                // 高级升级: 铱锭(337)x2, 电池组(787)x1
                new RecipeModel("Advanced Storage Upgrade", UpgradeT2Id, 1)
                    .AddIngredient("337", 2)
                    .AddIngredient("787", 1),

                // 量子升级: 放射性矿锭(910)x1, 电池组(787)x5
                new RecipeModel("Quantum Storage Upgrade", UpgradeT3Id, 1)
                    .AddIngredient("910", 1)
                    .AddIngredient("787", 5)
            };
        }
    }
}
