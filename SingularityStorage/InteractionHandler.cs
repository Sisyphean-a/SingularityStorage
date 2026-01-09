using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SingularityStorage.UI;
using SingularityStorage.Framework.Content;

namespace SingularityStorage
{
    public class InteractionHandler
    {
        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;
        
        // 定义自定义对象的限定物品 ID。
        // 格式：(BC)ModID_ItemId
        // 定义自定义对象的限定物品 ID。
        // 格式：(BC)ModID_ItemId
        private const string SingularityChestQualifiedId = $"(BC){ItemDefinitions.SingularityChestId}";

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
                // 在 1.6 中，我们检查 QualifiedItemId
                // 由于 CP 使用 ModId 作为前缀，我们需要匹配 ItemDefinitions 中的内容
                // manifest.json 的 ID 是 "Singularity.Storage"，ItemDefinitions 使用正确的 ID
                if (obj.QualifiedItemId == SingularityChestQualifiedId || obj.ItemId == ItemDefinitions.SingularityChestId)
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
            if (item.ItemId == ItemDefinitions.UpgradeT1Id) increment = 36;
            else if (item.ItemId == ItemDefinitions.UpgradeT2Id) increment = 100;
            else if (item.ItemId == ItemDefinitions.UpgradeT3Id) increment = 999;
            
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
