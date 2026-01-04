using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SingularityStorage.UI;

namespace SingularityStorage
{
    public class InteractionHandler
    {
        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;
        
        // 定义自定义对象的限定物品 ID。
        // 格式：(BC)ModID_ItemId
        private const string SingularityChestId = "(BC)Singularity.Storage_SingularityChest";

        public InteractionHandler(IModHelper helper, IMonitor monitor)
        {
            this._helper = helper;
            this._monitor = monitor;

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // 仅处理右键点击（检查动作）
            if (!e.Button.IsActionButton()) return;

            var clickedTile = e.Cursor.Tile;
            // 检查点击的瓷砖处是否有对象
            if (Game1.currentLocation.Objects.TryGetValue(clickedTile, out var obj))
            {
                // 在 1.6 中，我们检查 QualifiedItemId
                // 由于 CP 使用 ModId 作为前缀，我们需要匹配 content.json 中的内容
                // manifest.json 的 ID 是 "Singularity.Storage"，content.json 使用 {{ModId}}_SingularityChest
                if (obj.QualifiedItemId == SingularityChestId || obj.ItemId == "Singularity.Storage_SingularityChest")
                {
                    // 抑制默认动作（可能只是播放声音或晃动）
                    this._helper.Input.Suppress(e.Button);

                    // 检查是否为升级物品
                    if (this.HandleUpgrade(obj, Game1.player.CurrentItem))
                    {
                        return;
                    }
                    
                    this.OpenStorage(obj);
                }
            }
        }

        private bool HandleUpgrade(StardewValley.Object chest, Item? item)
        {
            if (item == null) return false;
            
            // 定义升级数值
            var increment = 0;
            if (item.ItemId == "Singularity.Storage_T1_Comp") increment = 36;
            else if (item.ItemId == "Singularity.Storage_T2_Comp") increment = 100;
            else if (item.ItemId == "Singularity.Storage_T3_Comp") increment = 999;
            
            if (increment > 0)
            {
                // 确保 GUID 存在
                if (!chest.modData.ContainsKey("SingularityData_GUID"))
                {
                    chest.modData["SingularityData_GUID"] = Guid.NewGuid().ToString();
                }

                // 执行升级
                StorageManager.UpgradeCapacity(guid, increment);
                
                // 消耗物品
                Game1.player.reduceActiveItemByOne();
                
                // 反馈
                Game1.playSound("bubbles");
                Game1.addHUDMessage(new HUDMessage($"存储已升级！容量 +{increment}", 2));
                
                return true;
            }
            
            return false;
        }

        private void OpenStorage(StardewValley.Object chestObj)
        {
            // 确保箱子拥有 GUID
            if (!chestObj.modData.ContainsKey("SingularityData_GUID"))
            {
                chestObj.modData["SingularityData_GUID"] = Guid.NewGuid().ToString();
            }

            var guid = chestObj.modData["SingularityData_GUID"];
            
            this._monitor.Log($"正在打开奇点仓库：{guid}", LogLevel.Debug);
            
            // 打开 UI
            Game1.activeClickableMenu = new SingularityMenu(guid);
        }
    }
}
