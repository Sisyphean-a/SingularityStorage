using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Linq;

namespace SingularityStorage.UI.Controllers
{
    public class InventoryHandler
    {
        private readonly string _sourceGuid;
        private readonly InventoryMenu _storageInventory;
        private readonly InventoryMenu _playerInventory;
        private readonly Func<List<Item?>> _getFullInventory; // 获取最新数据源的回调
        private readonly Action _requestRefresh; // 刷新 UI 的回调

        public Item? HeldItem { get; set; }
        
        public InventoryHandler(string sourceGuid, InventoryMenu storageMenu, InventoryMenu playerMenu, 
            Func<List<Item?>> getFullInventory, Action requestRefresh)
        {
            this._sourceGuid = sourceGuid;
            this._storageInventory = storageMenu;
            this._playerInventory = playerMenu;
            this._getFullInventory = getFullInventory;
            this._requestRefresh = requestRefresh;
        }

        public Item? CheckForHover(int x, int y)
        {
             var hover = this._storageInventory.hover(x, y, null);
             if (hover == null)
                 hover = this._playerInventory.hover(x, y, null);
             return hover;
        }

        public bool HandleLeftClick(int x, int y, bool isShift)
        {
            // 处理仓库库存点击 (Storage Inventory)
            var clickedItem = this._storageInventory.getItemAt(x, y);
            if (clickedItem != null)
            {
                if (isShift)
                {
                     // Shift + 点击：转移给玩家 (Transfer to Player)
                     if (Game1.player.couldInventoryAcceptThisItem(clickedItem))
                     {
                         Game1.player.addItemToInventory(clickedItem);
                         this._storageInventory.actualInventory.Remove(clickedItem); // 视觉上的更新
                         
                         if (Context.IsMainPlayer)
                         {
                             StorageManager.RemoveItem(this._sourceGuid, clickedItem);
                             // 此处不直接修改 FullInventory，因为我们相信 Refresh 会在下一帧或通过回调重新加载。
                         }
                         Game1.playSound("dwop");
                         this._requestRefresh();
                     }
                     return true;
                }
                
                // 普通点击：拿起物品 (Pick up)
                if (this.HeldItem == null)
                {
                    this.HeldItem = clickedItem;
                    this._storageInventory.actualInventory.Remove(clickedItem);
                    
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.RemoveItem(this._sourceGuid, clickedItem);
                    }
                    
                    Game1.playSound("dwop");
                    this._requestRefresh(); 
                    return true;
                }
            }

            // 处理玩家背包点击 (Player Inventory)
            var playerItem = this._playerInventory.getItemAt(x, y);
            if (playerItem != null)
            {
                if (isShift)
                {
                    // Shift + 点击：转移到仓库 (Transfer to Storage)
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.AddItem(this._sourceGuid, playerItem);
                        Game1.player.removeItemFromInventory(playerItem);
                        
                        Game1.playSound("dwop");
                        this._requestRefresh();
                    }
                    return true;
                }
                
                // 普通点击：拿起物品 (Pick up)
                if (this.HeldItem == null)
                {
                    this.HeldItem = playerItem;
                    Game1.player.removeItemFromInventory(playerItem);
                    Game1.playSound("dwop");
                    return true;
                }
            }

            // 放置拿在手中的物品
            if (this.HeldItem != null)
            {
                // 尝试放入仓库
                if (this._storageInventory.isWithinBounds(x, y))
                {
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.AddItem(this._sourceGuid, this.HeldItem);
                    }

                    if (this.HeldItem.Stack <= 0)
                    {
                        this.HeldItem = null;
                        Game1.playSound("stoneStep");
                    }
                    else
                    {
                        // 物品未能完全添加（容量已满）
                        Game1.playSound("cancel");
                        Game1.addHUDMessage(new HUDMessage("Storage Full", 3));
                    }
                    
                    this._requestRefresh();
                    return true;
                }

                // 尝试放入玩家背包
                if (this._playerInventory.isWithinBounds(x, y))
                {
                    Game1.player.addItemToInventory(this.HeldItem);
                    this.HeldItem = null;
                    Game1.playSound("stoneStep");
                    return true;
                }
            }
            
            return false;
        }

        public bool HandleRightClick(int x, int y)
        {
            // 仓库逻辑：拿取一个 (Take One)
            var storageItem = this._storageInventory.getItemAt(x, y);
            if (storageItem != null)
            {
                // 如果手中没有拿东西 -> 拿取一个
                if (this.HeldItem == null)
                {
                    var single = storageItem.getOne();
                    this.HeldItem = single;
                    
                    // 减少仓库中的堆叠数量
                     storageItem.Stack--;
                     if (storageItem.Stack <= 0)
                     {
                         if (Context.IsMainPlayer) StorageManager.RemoveItem(this._sourceGuid, storageItem);
                     }
                    
                     Game1.playSound("dwop");
                     this._requestRefresh(); // 更新计数
                     return true;
                }
                else
                {
                    // 如果手中拿有东西 -> 如果匹配则放置一个 (Place one IF matches)
                    if (this.HeldItem.canStackWith(storageItem))
                    {
                        if (Context.IsMainPlayer)
                        {
                            var one = this.HeldItem.getOne();
                            this.HeldItem.Stack--;
                            if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                            
                            StorageManager.AddItem(this._sourceGuid, one);
                             
                             Game1.playSound("dwop");
                             this._requestRefresh();
                        }
                        return true;
                    }
                }
            }
            // 仓库逻辑：在空白槽位放置一个（或直接加入池中）
            else if (this._storageInventory.isWithinBounds(x, y))
            {
                if (this.HeldItem != null && Context.IsMainPlayer)
                {
                    var one = this.HeldItem.getOne();
                    this.HeldItem.Stack--;
                    if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                    
                    StorageManager.AddItem(this._sourceGuid, one);
                     
                     Game1.playSound("dwop");
                     this._requestRefresh();
                     return true;
                }
            }

            // 玩家背包逻辑：拆分 / 放置一个 (Split / Place One)
            var playerItem = this._playerInventory.getItemAt(x, y);
            if (playerItem != null)
            {
                 if (this.HeldItem == null)
                 {
                     var single = playerItem.getOne();
                     this.HeldItem = single;
                     playerItem.Stack--;
                     if (playerItem.Stack <= 0) Game1.player.removeItemFromInventory(playerItem);
                     Game1.playSound("dwop");
                 }
                 else
                 {
                     if (this.HeldItem.canStackWith(playerItem))
                     {
                         if (playerItem.getRemainingStackSpace() > 0)
                         {
                             playerItem.Stack++;
                             this.HeldItem.Stack--;
                             if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                             Game1.playSound("dwop");
                         }
                     }
                 }
                 return true;
            }
            else if (this._playerInventory.isWithinBounds(x, y))
            {
                if (this.HeldItem != null)
                {
                   var slot = this._playerInventory.getInventoryPositionOfClick(x, y);
                   if (slot != -1)
                   {
                        var one = this.HeldItem.getOne();
                        Game1.player.addItemToInventory(one, slot);
                         this.HeldItem.Stack--;
                        if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                        Game1.playSound("dwop");
                   }
                }
                return true;
            }
            
            return false;
        }

        public void ReturnHeldItem()
        {
            if (this.HeldItem != null)
            {
                if (Game1.player.couldInventoryAcceptThisItem(this.HeldItem))
                {
                    Game1.player.addItemToInventory(this.HeldItem);
                }
                else
                {
                    Game1.createItemDebris(this.HeldItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
                }
                this.HeldItem = null;
            }
        }

        public void FillExistingStacks()
        {
            var fullInventory = this._getFullInventory();
            var playerItems = Game1.player.Items.Where(i => i != null).ToList();
            var changed = false;

            foreach (var pItem in playerItems.OfType<Item>()
                         .Select(pItem => new
                         {
                             pItem,
                             exists = fullInventory.Any(sItem => sItem != null && sItem.canStackWith(pItem))
                         })
                         .Where(t => t.exists)
                         .Where(_ => Context.IsMainPlayer)
                         .Select(t => t.pItem))
            {
                StorageManager.AddItem(_sourceGuid, pItem);
                Game1.player.removeItemFromInventory(pItem);
                changed = true;
            }

            if (!changed || !Context.IsMainPlayer) return;
            Game1.playSound("Ship");
            this._requestRefresh();
        }

        public void StoreAll()
        {
            var playerItems = Game1.player.Items.Where(i => i != null).ToList();
            var changed = false;

            foreach (var pItem in playerItems.OfType<Item>()
                         .Where(_ => Context.IsMainPlayer))
            {
                StorageManager.AddItem(_sourceGuid, pItem);
                Game1.player.removeItemFromInventory(pItem);
                changed = true;
            }

            if (!changed || !Context.IsMainPlayer) return;
            Game1.playSound("Ship");
            this._requestRefresh();
        }
    }
}
