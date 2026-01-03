using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SingularityStorage.UI.Controllers
{
    public class InventoryHandler
    {
        private readonly string _sourceGuid;
        private readonly InventoryMenu _storageInventory;
        private readonly InventoryMenu _playerInventory;
        private readonly Func<List<Item?>> _getFullInventory; // Callback to get fresh data source
        private readonly Action _requestRefresh; // Callback to refresh UI

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
            // Handle Storage Inventory clicks
            var clickedItem = this._storageInventory.getItemAt(x, y);
            if (clickedItem != null)
            {
                if (isShift)
                {
                     // Shift + Click: Transfer to Player
                     if (Game1.player.couldInventoryAcceptThisItem(clickedItem))
                     {
                         Game1.player.addItemToInventory(clickedItem);
                         this._storageInventory.actualInventory.Remove(clickedItem); // Visual update
                         
                         if (Context.IsMainPlayer)
                         {
                             StorageManager.RemoveItem(this._sourceGuid, clickedItem);
                             // We don't modify FullInventory directly here, trusting Refresh to reload it next frame or callback
                         }
                         Game1.playSound("dwop");
                         this._requestRefresh();
                     }
                     return true;
                }
                
                // Normal Click: Pick up
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

            // Handle Player Inventory clicks
            var playerItem = this._playerInventory.getItemAt(x, y);
            if (playerItem != null)
            {
                if (isShift)
                {
                    // Shift + Click: Transfer to Storage
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.AddItem(this._sourceGuid, playerItem);
                        Game1.player.removeItemFromInventory(playerItem);
                        
                        Game1.playSound("dwop");
                        this._requestRefresh();
                    }
                    return true;
                }
                
                // Normal Click: Pick up
                if (this.HeldItem == null)
                {
                    this.HeldItem = playerItem;
                    Game1.player.removeItemFromInventory(playerItem);
                    Game1.playSound("dwop");
                    return true;
                }
            }

            // Place held item
            if (this.HeldItem != null)
            {
                // Try to place in storage
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
                        // Item was not fully added (capacity full)
                        Game1.playSound("cancel");
                        Game1.addHUDMessage(new HUDMessage("Storage Full", 3));
                    }
                    
                    this._requestRefresh();
                    return true;
                }

                // Try to place in player inventory
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
            // Storage Logic: Take One
            var storageItem = this._storageInventory.getItemAt(x, y);
            if (storageItem != null)
            {
                // If holding nothing -> Take one
                if (this.HeldItem == null)
                {
                    var single = storageItem.getOne();
                    this.HeldItem = single;
                    
                    // Reduce stack in storage
                     storageItem.Stack--;
                     if (storageItem.Stack <= 0)
                     {
                         if (Context.IsMainPlayer) StorageManager.RemoveItem(this._sourceGuid, storageItem);
                     }
                    
                     Game1.playSound("dwop");
                     this._requestRefresh(); // Update counts
                     return true;
                }
                else
                {
                    // If holding something -> Place one IF matches
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
            // Storage Logic: Place One in Empty Slot (or just add to pool)
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

            // Player Inventory Logic: Split / Place One
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
    }
}
