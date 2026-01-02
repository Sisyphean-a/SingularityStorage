using System.Collections.Generic;
using StardewValley;

namespace SingularityStorage.Data
{
    public static class CategoryData
    {
        // Category Constants (as provided by user + official Stardew IDs)
        public const int GreensCategory = -81;
        public const int GemCategory = -2;
        public const int MineralsCategory = -12;
        public const int MetalResources = -15;
        public const int BuildingResources = -16;

        public const int VegetableCategory = -75;
        public const int FruitsCategory = -79;
        public const int FlowersCategory = -80;
        public const int SeedsCategory = -74;
        public const int ArtisanGoodsCategory = -26;
        public const int SyrupCategory = -27;
        public const int FertilizerCategory = -19;

        public const int EggCategory = -5;
        public const int MilkCategory = -6;
        public const int MeatCategory = -14;

        public const int CookingCategory = -7;
        public const int IngredientsCategory = -25;
        public const int Inedible = -300;

        public const int FishCategory = -4;
        public const int BaitCategory = -21;
        public const int TackleCategory = -22;
        public const int SellAtFishShopCategory = -23;

        public const int CraftingCategory = -8;
        public const int BigCraftableCategory = -9;
        public const int FurnitureCategory = -24;

        public const int ToolCategory = -99;
        public const int WeaponCategory = -98;
        public const int BootsCategory = -97;
        public const int HatCategory = -95;
        public const int RingCategory = -96;
        public const int ClothingCategory = -100;
        public const int TrinketCategory = -101; // 1.6

        public const int BooksCategory = -102; // 1.6
        public const int SkillBooksCategory = -103; // 1.6
        public const int MonsterLootCategory = -28;
        public const int JunkCategory = -20;
        public const int LitterCategory = -999;

        // Group Definitions
        // Used List<int> to preserve display order of sub-categories
        public static readonly Dictionary<string, List<int>> CategoryGroups = new Dictionary<string, List<int>>
        {
            { "基础资源", new List<int> { GreensCategory, GemCategory, MineralsCategory, MetalResources, BuildingResources } },
            { "农产品", new List<int> { VegetableCategory, FruitsCategory, FlowersCategory, SeedsCategory, ArtisanGoodsCategory, SyrupCategory, FertilizerCategory } },
            { "动物产品", new List<int> { EggCategory, MilkCategory, MeatCategory } },
            { "食物烹饪", new List<int> { CookingCategory, IngredientsCategory, Inedible } },
            { "钓鱼", new List<int> { FishCategory, BaitCategory, TackleCategory, SellAtFishShopCategory } },
            { "制造设备", new List<int> { CraftingCategory, BigCraftableCategory, FurnitureCategory } },
            { "装备工具", new List<int> { ToolCategory, WeaponCategory, BootsCategory, HatCategory, RingCategory, ClothingCategory, TrinketCategory } },
            { "书籍特殊", new List<int> { BooksCategory, SkillBooksCategory, MonsterLootCategory, JunkCategory, LitterCategory } }
        };

        public static readonly Dictionary<int, string> CategoryNames = new Dictionary<int, string>
        {
            { GreensCategory, "采集物" },
            { GemCategory, "宝石" },
            { MineralsCategory, "矿物" },
            { MetalResources, "金属资源" },
            { BuildingResources, "建筑资源" },
            { VegetableCategory, "蔬菜" },
            { FruitsCategory, "水果" },
            { FlowersCategory, "花卉" },
            { SeedsCategory, "种子" },
            { ArtisanGoodsCategory, "工匠物品" },
            { SyrupCategory, "树液" },
            { FertilizerCategory, "肥料" },
            { EggCategory, "蛋类" },
            { MilkCategory, "奶类" },
            { MeatCategory, "肉类" },
            { CookingCategory, "烹饪" },
            { IngredientsCategory, "原料" },
            { Inedible, "不可食用" },
            { FishCategory, "鱼类" },
            { BaitCategory, "鱼饵" },
            { TackleCategory, "渔具" },
            { SellAtFishShopCategory, "鱼店商品" },
            { CraftingCategory, "手工物品" },
            { BigCraftableCategory, "大型制造" },
            { FurnitureCategory, "家具" },
            { ToolCategory, "工具" },
            { WeaponCategory, "武器" },
            { BootsCategory, "靴子" },
            { HatCategory, "帽子" },
            { RingCategory, "戒指" },
            { ClothingCategory, "服装" },
            { TrinketCategory, "饰品" },
            { BooksCategory, "书籍" },
            { SkillBooksCategory, "技能书" },
            { MonsterLootCategory, "怪物战利品" },
            { JunkCategory, "垃圾" },
            { LitterCategory, "杂物" }
        };

        public static readonly List<string> Tabs = new List<string>
        {
            "全部", // All
            "基础资源",
            "农产品",
            "动物产品",
            "食物烹饪",
            "钓鱼",
            "制造设备",
            "装备工具",
            "书籍特殊"
        };

        public static bool IsItemInGroup(Item? item, string groupName)
        {
            if (item == null) return false;
            if (groupName == "全部") return true;

            if (CategoryGroups.TryGetValue(groupName, out var categories))
            {
                return categories.Contains(item.Category);
            }

            return false;
        }

        public static string GetCategoryName(int category)
        {
            if (CategoryNames.TryGetValue(category, out var name))
            {
                return name;
            }
            return "未知";
        }
    }
}
