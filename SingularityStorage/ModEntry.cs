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
            
            // 初始化资源注册表
            new Framework.Content.ContentRegistry(this.Helper, this.Monitor, this.ModManifest.UniqueID);
            
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
