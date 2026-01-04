using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using HarmonyLib;

namespace SingularityStorage
{
    /// <summary>Mod 入口点。</summary>
    public class ModEntry : Mod
    {
        /*********
        ** 属性
        *********/
        /// <summary>此 Mod 的单例实例。</summary>
        public static ModEntry? Instance { get; private set; }

        /*********
        ** 公共方法
        *********/
        /// <summary>Mod 入口点，在 Mod 首次加载后调用。</summary>
        /// <param name="helper">提供用于编写 Mod 的简化 API。</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            // 初始化 Harmony
            this.Monitor.Log("Singularity Storage 正在初始化...", LogLevel.Info);

            // 初始化 Harmony 补丁
            var harmony = new Harmony(this.ModManifest.UniqueID);
            // 注册事件
            harmony.PatchAll();

            // 注册游戏事件
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            
            // 初始化管理器
            StorageManager.Initialize(this.Monitor, this.Helper.Data);
            
            // 初始化处理器
            new InteractionHandler(this.Helper, this.Monitor);
            
            // 初始化网络
            Network.NetworkManager.Initialize(this.Helper, this.Monitor);
        }

        /*********
        ** 私有方法
        *********/
        
        /// <summary>当请求资产时触发。</summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件数据。</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // 1. 加载纹理
            if (e.Name.IsEquivalentTo($"Mods/{this.ModManifest.UniqueID}/SingularityChest"))
            {
                e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/SingularityChest.png", AssetLoadPriority.Medium);
            }
            // 加载各个升级项的纹理
            if (e.Name.IsEquivalentTo($"Mods/{this.ModManifest.UniqueID}/Upgrade_T1"))
            {
                e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/SilverProcessor.png", AssetLoadPriority.Medium);
            }
            if (e.Name.IsEquivalentTo($"Mods/{this.ModManifest.UniqueID}/Upgrade_T2"))
            {
                e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/GoldCircuit.png", AssetLoadPriority.Medium);
            }
            if (e.Name.IsEquivalentTo($"Mods/{this.ModManifest.UniqueID}/Upgrade_T3"))
            {
                e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/VoidRune.png", AssetLoadPriority.Medium);
            }

            // 2. 添加 BigCraftable 数据
            if (e.Name.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;
                    var itemData = new StardewValley.GameData.BigCraftables.BigCraftableData
                    {
                        Name = "SingularityChest", 
                        DisplayName = "奇点仓库", 
                        Price = 1000,
                        Description = "拥有虚拟化存储空间的箱子。",
                        Texture = $"Mods/{this.ModManifest.UniqueID}/SingularityChest",
                        SpriteIndex = 0,
                        Fragility = 0,
                        CanBePlacedOutdoors = true
                    };
                    data["Singularity.Storage_SingularityChest"] = itemData;
                });
            }
            
            // 2.1 添加物品数据
            if (e.Name.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, StardewValley.GameData.Objects.ObjectData>().Data;
                    
                    data["Singularity.Storage_T1_Comp"] = new StardewValley.GameData.Objects.ObjectData
                    {
                        Name = "Singularity.Storage_T1_Comp",
                        DisplayName = "基础存储升级",
                        Description = "为奇点仓库增加 36 个槽位。",
                        Type = "Basic",
                        Category = -16,
                        Price = 1000,
                        Texture = $"Mods/{this.ModManifest.UniqueID}/Upgrade_T1",
                        SpriteIndex = 0
                    };
                    
                    data["Singularity.Storage_T2_Comp"] = new StardewValley.GameData.Objects.ObjectData
                    {
                        Name = "Singularity.Storage_T2_Comp",
                        DisplayName = "高级存储升级",
                        Description = "为奇点仓库增加 100 个槽位。",
                        Type = "Basic",
                        Category = -16,
                        Price = 5000,
                        Texture = $"Mods/{this.ModManifest.UniqueID}/Upgrade_T2",
                        SpriteIndex = 0
                    };
                    
                    data["Singularity.Storage_T3_Comp"] = new StardewValley.GameData.Objects.ObjectData
                    {
                        Name = "Singularity.Storage_T3_Comp",
                        DisplayName = "量子存储升级",
                        Description = "为奇点仓库增加 999 个槽位。",
                        Type = "Basic",
                        Category = -16,
                        Price = 20000,
                        Texture = $"Mods/{this.ModManifest.UniqueID}/Upgrade_T3",
                        SpriteIndex = 0
                    };
                });
            }

            // 3. 添加制作配方
            if (e.Name.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, string>().Data;
                    // 格式：材料 / 输出数量 / 输出 ID / 类型 / 技能解锁
                    // 铱锭 (337) x10, 金锭 (336) x5, 电池组 (787) x1
                    data["Singularity Chest"] = "337 1 336 5 787 1/Home/Singularity.Storage_SingularityChest/true/default"; 
                    data["Basic Storage Upgrade"] = "338 5 335 5/Home/Singularity.Storage_T1_Comp/false/default";
                    data["Advanced Storage Upgrade"] = "337 2 787 1/Home/Singularity.Storage_T2_Comp/false/default";
                    data["Quantum Storage Upgrade"] = "910 1 787 5/Home/Singularity.Storage_T3_Comp/false/default";
                });
            }
        }

        /// <summary>在玩家加载存档后触发。</summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件数据。</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.Monitor.Log("存档已加载。正在初始化存储系统...", LogLevel.Debug);
            StorageManager.ClearCache();
            
            // 确保玩家已知晓配方
            if (!Game1.player.craftingRecipes.ContainsKey("Singularity Chest"))
            {
                Game1.player.craftingRecipes.Add("Singularity Chest", 0);
            }
        }

        /// <summary>在游戏保存数据前触发。</summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件数据。</param>
        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Monitor.Log("正在保存数据...", LogLevel.Debug);
            StorageManager.SaveAll();
        }
    }
}
