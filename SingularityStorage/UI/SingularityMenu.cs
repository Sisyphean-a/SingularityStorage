using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using SingularityStorage.Network;

namespace SingularityStorage.UI
{
    public class SingularityMenu : IClickableMenu
    {
        // Configuration
        private static MenuConfig? _config;
        private static MenuConfig Config
        {
            get
            {
                if (_config == null)
                {
                    string configPath = Path.Combine(ModEntry.Instance!.Helper.DirectoryPath, "UI", "MenuConfig.json");
                    _config = MenuConfig.Load(configPath);
                }
                return _config;
            }
        }

        // Core Data
        private string SourceGuid;
        private List<Item?> FullInventory = new List<Item?>(); 
        private List<Item?> FilteredInventory = new List<Item?>();
        private int CurrentPage = 0;
        private int ItemsPerPage => Config.StorageInventory.Columns * Config.StorageInventory.Rows;

        // Categories
        private List<ClickableComponent> CategoryTabs = new List<ClickableComponent>();
        private string CurrentCategory = "All";
        private readonly List<string> Categories = new List<string> { "All", "Weapons", "Tools", "Resources", "Misc" };
        private string CachedCapacityText = "";

        // UI Components
        private InventoryMenu StorageInventory;
        private InventoryMenu PlayerInventory;
        private ClickableTextureComponent? NextPageButton;
        private ClickableTextureComponent? PrevPageButton;
        private TextBox? SearchBar;
        private ClickableTextureComponent? OkButton;
        private ClickableTextureComponent? FillStacksButton;
        
        // State
        private string LastSearchText = "";
        private bool IsLoading = false;
        private Item? HoverItem;
        private Item? HeldItem;

        public SingularityMenu(string guid) : base()
        {
            this.SourceGuid = guid;

            // Load dimensions from config
            this.width = Config.MenuDimensions.Width;
            this.height = Config.MenuDimensions.Height;
            this.xPositionOnScreen = (Game1.uiViewport.Width - this.width) / 2;
            this.yPositionOnScreen = (Game1.uiViewport.Height - this.height) / 2;

            // Initialize Storage Inventory from config
            this.StorageInventory = new InventoryMenu(
                this.xPositionOnScreen + Config.StorageInventory.OffsetX,
                this.yPositionOnScreen + Config.StorageInventory.OffsetY,
                false,
                new List<Item>(),
                null,
                ItemsPerPage,
                Config.StorageInventory.Rows,
                Config.StorageInventory.SlotSpacing,
                Config.StorageInventory.SlotSpacing,
                true
            );

            // Initialize Player Inventory from config
            this.PlayerInventory = new InventoryMenu(
                this.xPositionOnScreen + Config.PlayerInventory.OffsetX,
                this.yPositionOnScreen + this.height - Config.PlayerInventory.OffsetFromBottom,
                true
            );

            this.InitializeWidgets();

            if (Context.IsMainPlayer)
            {
                var data = StorageManager.GetInventory(guid);
                this.FullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                this.FilteredInventory = this.FullInventory;
                this.RefreshView();
            }
            else
            {
                this.IsLoading = true;
                NetworkManager.SendRequestView(guid, 0, "");
            }
        }

        private void InitializeWidgets()
        {
            int headerY = this.yPositionOnScreen + Config.Header.OffsetY;

            // Search Bar from config
            this.SearchBar = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"), 
                null, 
                Game1.smallFont, 
                Game1.textColor)
            {
                X = this.xPositionOnScreen + Config.SearchBar.OffsetX,
                Y = headerY,
                Width = Config.SearchBar.Width,
                Height = Config.SearchBar.Height
            };

            // Page Buttons from config
            this.PrevPageButton = new ClickableTextureComponent(
                new Rectangle(
                    this.xPositionOnScreen + Config.PageButtons.PrevOffsetX,
                    headerY,
                    Config.PageButtons.Width,
                    Config.PageButtons.Height),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                4f);

            this.NextPageButton = new ClickableTextureComponent(
                new Rectangle(
                    this.xPositionOnScreen + Config.PageButtons.NextOffsetX,
                    headerY,
                    Config.PageButtons.Width,
                    Config.PageButtons.Height),
                Game1.mouseCursors,
                new Rectangle(365, 495, 12, 11),
                4f);

            // Fill Stacks Button
            if (Config.FillStacksButton != null)
            {
                 Rectangle srcRect = new Rectangle(103, 469, 16, 16);
                 if (Config.FillStacksButton.TextureSource != null)
                 {
                     srcRect = new Rectangle(
                         Config.FillStacksButton.TextureSource.X, 
                         Config.FillStacksButton.TextureSource.Y, 
                         Config.FillStacksButton.TextureSource.Width, 
                         Config.FillStacksButton.TextureSource.Height);
                 }
                 
                 float scale = Config.FillStacksButton.Size / (float)srcRect.Width;
                 
                 // Position: Right side of header area, next to search bar
                 int buttonX = this.xPositionOnScreen + this.width - Config.FillStacksButton.OffsetFromRight - Config.FillStacksButton.Size;
                 int buttonY = this.yPositionOnScreen + Config.Header.OffsetY + 8; // Align with header
                 
                 this.FillStacksButton = new ClickableTextureComponent(
                    new Rectangle(
                        buttonX,
                        buttonY,
                        Config.FillStacksButton.Size,
                        Config.FillStacksButton.Size),
                    Game1.mouseCursors,
                    srcRect,
                    scale);
                    
                 ModEntry.Instance.Monitor.Log($"FillStacksButton created at ({buttonX}, {buttonY}), size: {Config.FillStacksButton.Size}", StardewModdingAPI.LogLevel.Debug);
            }
            else
            {
                ModEntry.Instance.Monitor.Log("FillStacksButton config is NULL!", StardewModdingAPI.LogLevel.Warn);
            }

            // OK Button from config
            this.OkButton = new ClickableTextureComponent(
                new Rectangle(
                    this.xPositionOnScreen + this.width - Config.OkButton.OffsetFromRight,
                    this.yPositionOnScreen + this.height - Config.OkButton.OffsetFromBottom,
                    Config.OkButton.Size,
                    Config.OkButton.Size),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f);

            // Initialize Category Tabs
            this.CategoryTabs.Clear();
            int tabX = this.xPositionOnScreen - 64; // Left of menu
            int tabY = this.yPositionOnScreen + 64;
            
            for (int i = 0; i < this.Categories.Count; i++)
            {
                this.CategoryTabs.Add(new ClickableComponent(
                    new Rectangle(tabX, tabY + (i * 64), 64, 64), 
                    this.Categories[i]));
            }
        }

        private void RefreshView()
        {
            if (!Context.IsMainPlayer)
            {
                this.IsLoading = true;
                NetworkManager.SendRequestView(this.SourceGuid, this.CurrentPage, this.SearchBar?.Text ?? "");
                return;
            }

            int totalItems = this.FilteredInventory.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)ItemsPerPage);
            if (totalPages == 0) totalPages = 1;

            if (this.CurrentPage >= totalPages) this.CurrentPage = totalPages - 1;
            if (this.CurrentPage < 0) this.CurrentPage = 0;

            int startIndex = this.CurrentPage * ItemsPerPage;
            var pageItems = this.FilteredInventory
                .Skip(startIndex)
                .Take(ItemsPerPage)
                .ToList();

            this.StorageInventory.actualInventory = pageItems.Cast<Item>().ToList();
            
            // Pad with nulls to fill the page, ensuring 36 slots appear
            // Actually, InventoryMenu handles partial lists by just showing them.
            // If the user wants to see empty slots, we might need to ensure the capacity limit visual is handled?
            // The issue "cannot page flip" is because we are taking `ItemsPerPage` items.
            // If FilteredInventory.Count > ItemsPerPage, pagination should work.
            // Let's debug totalItems:
            // int totalItems = this.FilteredInventory.Count;
            
            // Update Capacity Text
           var (used, max) = StorageManager.GetCounts(this.SourceGuid);
           this.CachedCapacityText = $"{used} / {max}";
           
           ModEntry.Instance.Monitor.Log($"RefreshView: Total={totalItems}, Pages={totalPages}, CurrentPage={this.CurrentPage}, Capacity={used}/{max}", LogLevel.Trace);
        }

        private void UpdateSearch()
        {
            // Only re-apply filter if query changed
            string query = this.SearchBar?.Text?.Trim() ?? "";
            if (query != this.LastSearchText)
            {
                 // ApplyFilters handles the logic and update
                 this.ApplyFilters();
            }
            // ELSE: Do nothing. This prevents resetting CurrentPage to 0 every frame.
        }

        private void ApplyFilters()
        {
            string query = this.SearchBar?.Text?.Trim() ?? "";
            this.LastSearchText = query;
            this.CurrentPage = 0; // Reset page when filter applied

            if (Context.IsMainPlayer)
            {
                IEnumerable<Item?> items = this.FullInventory;

                // 1. Filter by Category
                if (this.CurrentCategory != "All")
                {
                    items = items.Where(item => IsItemInCategory(item, this.CurrentCategory));
                }

                // 2. Filter by Search
                if (!string.IsNullOrEmpty(query))
                {
                    items = items.Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
                }

                this.FilteredInventory = items.ToList();
            }
            
            this.RefreshView();
        }

        private bool IsItemInCategory(Item? item, string category)
        {
            if (item == null) return false;
            switch (category)
            {
                case "Weapons":
                    return item.Category == StardewValley.Object.weaponCategory;
                case "Tools":
                    return item is Tool && item.Category != StardewValley.Object.weaponCategory;
                case "Resources":
                    return item.Category == StardewValley.Object.GemCategory || 
                           item.Category == StardewValley.Object.mineralsCategory ||
                           item.Category == StardewValley.Object.metalResources ||
                           item.Category == StardewValley.Object.buildingResources ||
                           item.Category == StardewValley.Object.CraftingCategory ||
                           item.Category == StardewValley.Object.fertilizerCategory;
                case "Misc":
                     return !IsItemInCategory(item, "Weapons") && 
                            !IsItemInCategory(item, "Tools") && 
                            !IsItemInCategory(item, "Resources");
                default:
                    return true;
            }
        }
        
        private void FillExistingStacks()
        {
            if (!Context.IsMainPlayer)
            {
                // Network packet: RequestFillStacks
                // Not implemented yet on backend, so we might skip or log warning.
                // Assuming we can send a custom message or just "Add" remaining compatible items logic locally then sync? 
                // No, complex operations should be server-side or iterative.
                // For now, let's implement the iterated client-side logic for simplicity, 
                // though it might spam packets.
                // Better approach: Iterate locally, find matches, send Add requests.
            }
            
            // Logic: Scan Player Inventory. If item exists in Storage, move it.
            // Note: Efficient matching requires knowing Storage contents.
            // We have FullInventory locally (replicated).
            
            var playerItems = Game1.player.Items.Where(i => i != null).ToList();
            bool changed = false;

            foreach (var pItem in playerItems)
            {
                if (pItem == null) continue;
                
                // Check if similar item exists in Storage
                // Standard stacking rules: check CheckCanAddToStack() if possible, or just compare Names/IDs/Quality
                bool exists = this.FullInventory.Any(sItem => 
                    sItem != null && sItem.canStackWith(pItem));
                
                if (exists)
                {
                    // Move item to storage
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.AddItem(this.SourceGuid, pItem);
                        Game1.player.removeItemFromInventory(pItem);
                        changed = true;
                    }
                    else
                    {
                        // TODO: Network implementation for fill stacks
                        // For now, just try to move it
                        // StorageManager.AddItem won't work for farmhand directly if it writes disk.
                        // Farmhand needs to send packet.
                        // NetworkManager.SendTransfer(...);
                    }
                }
            }

            if (changed && Context.IsMainPlayer)
            {
                Game1.playSound("Ship");
                // Refresh
                 var data = StorageManager.GetInventory(this.SourceGuid);
                 this.FullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                 this.ApplyFilters();
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (this.PrevPageButton != null && this.PrevPageButton.containsPoint(x, y))
            {
                if (this.CurrentPage > 0)
                {
                    this.CurrentPage--;
                    Game1.playSound("shwip");
                    this.RefreshView();
                }
                return;
            }

            if (this.NextPageButton != null && this.NextPageButton.containsPoint(x, y))
            {
                int totalItems = this.FilteredInventory.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)ItemsPerPage);
                if (this.CurrentPage < totalPages - 1)
                {
                    this.CurrentPage++;
                    Game1.playSound("shwip");
                    this.RefreshView();
                }
                return;
            }

            if (this.OkButton != null && this.OkButton.containsPoint(x, y))
            {
                this.exitThisMenu();
                return;
            }

            if (this.FillStacksButton != null && this.FillStacksButton.containsPoint(x, y))
            {
                this.FillExistingStacks();
                return;
            }

            // Handle Category Tabs
            foreach (var tab in this.CategoryTabs)
            {
                if (tab.containsPoint(x, y))
                {
                    if (this.CurrentCategory != tab.name)
                    {
                        this.CurrentCategory = tab.name;
                        this.ApplyFilters(); // ApplyFilters resets page to 0
                        Game1.playSound("smallSelect");
                    }
                    return;
                }
            }
            
            bool isShift = (Game1.oldKBState.IsKeyDown(Keys.LeftShift) || Game1.oldKBState.IsKeyDown(Keys.RightShift));

            // Handle Storage Inventory clicks
            Item? clickedItem = this.StorageInventory.getItemAt(x, y);
            if (clickedItem != null)
            {
                if (isShift)
                {
                     // Shift + Click: Transfer to Player
                     if (Game1.player.couldInventoryAcceptThisItem(clickedItem))
                     {
                         Game1.player.addItemToInventory(clickedItem);
                         this.StorageInventory.actualInventory.Remove(clickedItem); // Visual update
                         
                         if (Context.IsMainPlayer)
                         {
                             StorageManager.RemoveItem(this.SourceGuid, clickedItem);
                             this.FullInventory.Remove(clickedItem);
                             this.FilteredInventory.Remove(clickedItem);
                             // Re-apply filters? Efficiency trade-off. 
                             // If we remove from filtered list, we should be fine.
                         }
                         Game1.playSound("dwop");
                         this.RefreshView();
                     }
                     return;
                }
                
                // Normal Click: Pick up
                if (this.HeldItem == null)
                {
                    this.HeldItem = clickedItem;
                    this.StorageInventory.actualInventory.Remove(clickedItem);
                    this.FullInventory.Remove(clickedItem);
                    this.FilteredInventory.Remove(clickedItem);
                    
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.RemoveItem(this.SourceGuid, clickedItem);
                    }
                    
                    Game1.playSound("dwop");
                    this.RefreshView(); 
                    return;
                }
            }

            // Handle Player Inventory clicks
            Item? playerItem = this.PlayerInventory.getItemAt(x, y);
            if (playerItem != null)
            {
                if (isShift)
                {
                    // Shift + Click: Transfer to Storage
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.AddItem(this.SourceGuid, playerItem);
                        Game1.player.removeItemFromInventory(playerItem);
                        
                        // Reload data
                        var data = StorageManager.GetInventory(this.SourceGuid);
                        this.FullInventory = data.Inventory.Values.SelectMany(dx => dx).Cast<Item?>().ToList();
                        this.ApplyFilters();
                        
                        Game1.playSound("dwop");
                    }
                    return;
                }
                
                // Normal Click: Pick up
                if (this.HeldItem == null)
                {
                    this.HeldItem = playerItem;
                    Game1.player.removeItemFromInventory(playerItem);
                    Game1.playSound("dwop");
                    return;
                }
            }

            // Place held item
            if (this.HeldItem != null)
            {
                // Try to place in storage
                if (this.StorageInventory.isWithinBounds(x, y))
                {
                    if (Context.IsMainPlayer)
                    {
                        ModEntry.Instance.Monitor.Log($"Attempting to add item: {this.HeldItem.Name} x{this.HeldItem.Stack}", LogLevel.Trace);
                        StorageManager.AddItem(this.SourceGuid, this.HeldItem);
                        
                        // Reload data from StorageManager to avoid duplicates
                        var data = StorageManager.GetInventory(this.SourceGuid);
                        this.FullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                        
                        // Re-apply filters to ensure view is consistent
                        // Note: ApplyFilters resets page to 0. 
                        // If we are on page 5 and add an item, we jump to page 0? That's annoying.
                        // We should try to stay on page.
                        
                        // 1. Filter by Category
                        IEnumerable<Item?> items = this.FullInventory;
                        if (this.CurrentCategory != "All") items = items.Where(item => IsItemInCategory(item, this.CurrentCategory));
                        string query = this.SearchBar?.Text?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(query)) items = items.Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
                        this.FilteredInventory = items.ToList();
                        
                        // Don't call ApplyFilters() because it resets page.
                    }

                    if (this.HeldItem.Stack <= 0)
                    {
                        ModEntry.Instance.Monitor.Log("Item fully consumed.", LogLevel.Trace);
                        this.HeldItem = null;
                        Game1.playSound("stoneStep");
                    }
                    else
                    {
                        // Item was not fully added (capacity full)
                        ModEntry.Instance.Monitor.Log("Storage Full - Item rejected/returned.", LogLevel.Warn);
                        Game1.playSound("cancel");
                        Game1.addHUDMessage(new HUDMessage("Storage Full", 3));
                    }
                    
                    this.RefreshView();
                    return;
                }

                // Try to place in player inventory
                if (this.PlayerInventory.isWithinBounds(x, y))
                {
                    Game1.player.addItemToInventory(this.HeldItem);
                    this.HeldItem = null;
                    Game1.playSound("stoneStep");
                    return;
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            // Do NOT call base.base... because we want to override default logic
            // base.receiveRightClick(x, y, playSound); 
            
            // Check Widgets first
            if (this.PrevPageButton != null && this.PrevPageButton.containsPoint(x, y)) { this.receiveLeftClick(x, y, playSound); return; }
            if (this.NextPageButton != null && this.NextPageButton.containsPoint(x, y)) { this.receiveLeftClick(x, y, playSound); return; }

            // Storage Logic: Take One
            Item? storageItem = this.StorageInventory.getItemAt(x, y);
            if (storageItem != null)
            {
                // If holding nothing -> Take one
                if (this.HeldItem == null)
                {
                    Item single = storageItem.getOne();
                    this.HeldItem = single;
                    
                    // Reduce stack in storage
                     storageItem.Stack--;
                     if (storageItem.Stack <= 0)
                     {
                         this.StorageInventory.actualInventory.Remove(storageItem);
                         this.FullInventory.Remove(storageItem);
                         if (Context.IsMainPlayer) StorageManager.RemoveItem(this.SourceGuid, storageItem);
                     }
                     else
                     {
                         // Just update count
                         // NOTE: StorageManager might need explicit update if it syncs by reference?
                         // If 'storageItem' is a reference to the object in StorageManager's dictionary, we are good.
                         // But we should verify. 
                         // To be safe, we trigger a "save" or assumed reference.
                     }
                     
                     // Optimization: If stack > 0, we don't strictly need to RemoveItem. 
                     // But we should ensure the backend knows the count changed if it tracks "total items".
                     // Ideally StorageManager handles the item object reference.
                     Game1.playSound("dwop");
                     this.RefreshView(); // Update counts
                     return;
                }
                else
                {
                    // If holding something -> Place one IF matches
                    if (this.HeldItem.canStackWith(storageItem))
                    {
                        // Standard chest: Right click reduces held stack by one, puts into chest
                        // Wait, Standard Right Click on Chest Item:
                        // 1. Holding Nothing + Right Click Chest Item = Take One.
                        // 2. Holding X + Right Click Chest Same Item = Place One.
                        
                        if (Context.IsMainPlayer)
                        {
                            Item one = this.HeldItem.getOne();
                            this.HeldItem.Stack--;
                            if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                            
                            StorageManager.AddItem(this.SourceGuid, one);
                             
                             // Update view
                             // To avoid full reload flickering, we can try to find the item object and increment?
                             // But StorageManager.AddItem handles merging. 
                             // So we reload.
                             var data = StorageManager.GetInventory(this.SourceGuid);
                             this.FullInventory = data.Inventory.Values.SelectMany(i => i).Cast<Item?>().ToList();
                             this.ApplyFilters();
                             
                             Game1.playSound("dwop");
                        }
                        return;
                    }
                }
            }
            // Storage Logic: Place One in Empty Slot
            else if (this.StorageInventory.isWithinBounds(x, y))
            {
                if (this.HeldItem != null)
                {
                    // Place one into new slot logic?
                    // "Singularity" storage doesn't really have "slots". It's a pool.
                    // Doing "Place One" into empty area just calls AddItem(One).
                    if (Context.IsMainPlayer)
                    {
                        Item one = this.HeldItem.getOne();
                        this.HeldItem.Stack--;
                        if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                        
                        StorageManager.AddItem(this.SourceGuid, one);
                        
                        // Reload
                         var data = StorageManager.GetInventory(this.SourceGuid);
                         this.FullInventory = data.Inventory.Values.SelectMany(i => i).Cast<Item?>().ToList();
                         this.ApplyFilters();
                         
                         Game1.playSound("dwop");
                    }
                    return;
                }
            }

            // Player Inventory Logic: Split / Place One
            Item? playerItem = this.PlayerInventory.getItemAt(x, y);
            if (playerItem != null)
            {
                // Holding Nothing + Right Click Player Item = Take One (or Split?)
                // Vanilla inventory Right Click on item = Take One.
                 if (this.HeldItem == null)
                 {
                     Item single = playerItem.getOne();
                     this.HeldItem = single;
                     playerItem.Stack--;
                     if (playerItem.Stack <= 0) Game1.player.removeItemFromInventory(playerItem);
                     Game1.playSound("dwop");
                 }
                 else
                 {
                     // Holding X + Right Click Same Item = Place One.
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
                 return;
            }
            else if (this.PlayerInventory.isWithinBounds(x, y))
            {
                // Place One into empty slot
                if (this.HeldItem != null)
                {
                    // Find actual slot
                   int slot = this.PlayerInventory.getInventoryPositionOfClick(x, y);
                   if (slot != -1)
                   {
                        // Add one to that slot? 
                        // Vanilla `utility.addItemToInventory` logic is complex.
                        // Simplified: Create new item of 1.
                        Item one = this.HeldItem.getOne();
                        Game1.player.addItemToInventory(one, slot);
                         this.HeldItem.Stack--;
                        if (this.HeldItem.Stack <= 0) this.HeldItem = null;
                        Game1.playSound("dwop");
                   }
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            
            this.HoverItem = this.StorageInventory.hover(x, y, null);
            if (this.HoverItem == null)
                this.HoverItem = this.PlayerInventory.hover(x, y, null);

            if (this.PrevPageButton != null)
                this.PrevPageButton.tryHover(x, y);
            if (this.NextPageButton != null)
                this.NextPageButton.tryHover(x, y);
            if (this.OkButton != null)
                this.OkButton.tryHover(x, y);
            if (this.FillStacksButton != null)
                this.FillStacksButton.tryHover(x, y);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            this.UpdateSearch();
        }

        public override void draw(SpriteBatch b)
        {
            // Draw fade overlay
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

            // Draw main menu background
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

            // Draw title from config
            string title = Config.Title.Text;
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, 
                new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + Config.Title.OffsetY), 
                Game1.textColor);

            // Draw header background from config
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                this.xPositionOnScreen + Config.Header.Padding, 
                this.yPositionOnScreen + Config.Header.OffsetY, 
                this.width - (Config.Header.Padding * 2), 
                Config.Header.Height, 
                Color.White, 4f, false);

            // Draw widgets
            this.SearchBar?.Draw(b);
            // Draw placeholder from config
            if (this.SearchBar != null && string.IsNullOrEmpty(this.SearchBar.Text) && !this.SearchBar.Selected)
            {
                 b.DrawString(Game1.smallFont, Config.SearchBar.Placeholder, new Vector2(this.SearchBar.X + 10, this.SearchBar.Y + 8), Color.Gray);
            }

            this.PrevPageButton?.draw(b);
            this.NextPageButton?.draw(b);

            // Draw page number
            if (this.PrevPageButton != null)
            {
                int totalPages = (int)Math.Ceiling(this.FilteredInventory.Count / (double)ItemsPerPage);
                if (totalPages == 0) totalPages = 1;
                string pageText = $"{this.CurrentPage + 1}/{totalPages}";
                Vector2 textSize = Game1.smallFont.MeasureString(pageText);
                float btnCenter = (this.PrevPageButton.bounds.Right + this.NextPageButton!.bounds.Left) / 2f;
                Utility.drawTextWithShadow(b, pageText, Game1.smallFont,
                    new Vector2(btnCenter - textSize.X / 2, this.PrevPageButton.bounds.Y + 12), Game1.textColor);
            }

            // Draw storage inventory
            this.StorageInventory.draw(b);

            // Draw separator from config
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                this.xPositionOnScreen + Config.Header.Padding, 
                this.PlayerInventory.yPositionOnScreen - Config.Separator.OffsetFromInventory, 
                this.width - (Config.Header.Padding * 2), 
                Config.Separator.Height, 
                Color.White, 4f, false);

            // Draw player inventory
            this.PlayerInventory.draw(b);

            // Draw OK button
            this.OkButton?.draw(b);

            // Draw Fill Stacks button (Text version for visibility)
            if (this.FillStacksButton != null)
            {
                // Draw button background
                bool isHovered = this.FillStacksButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY());
                Color bgColor = isHovered ? Color.Wheat : Color.White;
                
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                    this.FillStacksButton.bounds.X, this.FillStacksButton.bounds.Y,
                    this.FillStacksButton.bounds.Width, this.FillStacksButton.bounds.Height,
                    bgColor, 4f, false);
                
                // Draw text
                string buttonText = "填充";
                Vector2 textSize = Game1.smallFont.MeasureString(buttonText);
                Vector2 textPos = new Vector2(
                    this.FillStacksButton.bounds.X + (this.FillStacksButton.bounds.Width - textSize.X) / 2,
                    this.FillStacksButton.bounds.Y + (this.FillStacksButton.bounds.Height - textSize.Y) / 2);
                
                Utility.drawTextWithShadow(b, buttonText, Game1.smallFont, textPos, Game1.textColor);
                
                // Draw tooltip if hovered
                if (isHovered)
                {
                    IClickableMenu.drawToolTip(b, "将背包中已存在于箱子的物品全部存入", "填充堆叠", null);
                }
            }

            // Draw Category Tabs
            foreach (var tab in this.CategoryTabs)
            {
                // Draw background
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), 
                    tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, 
                    this.CurrentCategory == tab.name ? Color.White : Color.Gray, 4f, false);
                
                // Draw Text (First 3 chars)
                string label = tab.name.Length > 3 ? tab.name.Substring(0, 3) : tab.name;
                if (tab.name == "All") label = "ALL";
                
                Vector2 labelSize = Game1.smallFont.MeasureString(label);
                Utility.drawTextWithShadow(b, label, Game1.smallFont, 
                    new Vector2(tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2, tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2), 
                    Game1.textColor);
            }

            // Draw Capacity
            if (!string.IsNullOrEmpty(this.CachedCapacityText))
            {
                 Vector2 capSize = Game1.smallFont.MeasureString(this.CachedCapacityText);
                 Utility.drawTextWithShadow(b, this.CachedCapacityText, Game1.smallFont,
                     new Vector2(this.StorageInventory.xPositionOnScreen + this.StorageInventory.width - capSize.X, 
                                 this.StorageInventory.yPositionOnScreen + this.StorageInventory.height + 4), 
                     Color.White);
            }

            // Draw held item
            if (this.HeldItem != null)
            {
                this.HeldItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
            }

            // Draw hover text
            if (this.HoverItem != null && this.HeldItem == null)
            {
                IClickableMenu.drawToolTip(b, this.HoverItem.getDescription(), this.HoverItem.DisplayName, this.HoverItem);
            }

            // Draw loading text from config
            if (this.IsLoading)
            {
                Utility.drawTextWithShadow(b, Config.LoadingText.Text, Game1.smallFont,
                    new Vector2(this.StorageInventory.xPositionOnScreen, this.StorageInventory.yPositionOnScreen + Config.LoadingText.OffsetY), Color.Yellow);
            }

            // Draw cursor
            this.drawMouse(b);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            
            // Recalculate positions from config
            this.xPositionOnScreen = (Game1.uiViewport.Width - Config.MenuDimensions.Width) / 2;
            this.yPositionOnScreen = (Game1.uiViewport.Height - Config.MenuDimensions.Height) / 2;
            
            this.InitializeWidgets();
        }

        protected override void cleanupBeforeExit()
        {
            if (this.HeldItem != null)
            {
                // Try to return to inventory
                if (Game1.player.couldInventoryAcceptThisItem(this.HeldItem))
                {
                    Game1.player.addItemToInventory(this.HeldItem);
                }
                else
                {
                    // Inventory full, drop on ground
                    Game1.createItemDebris(this.HeldItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
                }
                this.HeldItem = null;
            }
            
            base.cleanupBeforeExit();
        }

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            if (packet.SourceGuid != this.SourceGuid) return;
            
            var pageItems = packet.Items ?? new List<Item?>();
            this.StorageInventory.actualInventory = pageItems.Cast<Item>().ToList();
            this.IsLoading = false;
        }
    }
}
