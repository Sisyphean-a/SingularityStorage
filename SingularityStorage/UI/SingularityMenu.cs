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

            // Handle Storage Inventory clicks
            Item? clickedItem = this.StorageInventory.getItemAt(x, y);
            if (clickedItem != null && this.HeldItem == null)
            {
                this.HeldItem = clickedItem;
                this.StorageInventory.actualInventory.Remove(clickedItem);
                
                // We don't remove from FullInventory/FilteredInventory immediately here?
                // Actually we do for consistency if we are "holding" it.
                // But if we drop it back, we re-add.
                // The issue is pagination. Removing from FilteredInventory shifts everything on next RefreshView.
                this.FullInventory.Remove(clickedItem);
                this.FilteredInventory.Remove(clickedItem);
                
                // Sync with StorageManager
                if (Context.IsMainPlayer)
                {
                    StorageManager.RemoveItem(this.SourceGuid, clickedItem);
                }
                
                Game1.playSound("dwop");
                this.RefreshView(); // Update view to fill gap
                return;
            }

            // Handle Player Inventory clicks
            Item? playerItem = this.PlayerInventory.getItemAt(x, y);
            if (playerItem != null && this.HeldItem == null)
            {
                this.HeldItem = playerItem;
                Game1.player.removeItemFromInventory(playerItem);
                Game1.playSound("dwop");
                return;
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
            base.receiveRightClick(x, y, playSound);
            
            // Quick transfer from storage to player
            Item? storageItem = this.StorageInventory.getItemAt(x, y);
            if (storageItem != null)
            {
                // Try to add to player inventory
                if (Game1.player.couldInventoryAcceptThisItem(storageItem))
                {
                    // Logic similar to Left Click
                    Game1.player.addItemToInventory(storageItem);
                    // this.StorageInventory.actualInventory.Remove(storageItem); // Handled by RefreshView?
                    
                    if (Context.IsMainPlayer)
                    {
                         StorageManager.RemoveItem(this.SourceGuid, storageItem);
                         
                         // Reload and Re-filter
                         var data = StorageManager.GetInventory(this.SourceGuid);
                         this.FullInventory = data.Inventory.Values.SelectMany(i => i).Cast<Item?>().ToList();
                         
                         // Re-filter manual
                        IEnumerable<Item?> items = this.FullInventory;
                        if (this.CurrentCategory != "All") items = items.Where(item => IsItemInCategory(item, this.CurrentCategory));
                        string query = this.SearchBar?.Text?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(query)) items = items.Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
                        this.FilteredInventory = items.ToList();
                    }
                    
                    this.RefreshView();
                    Game1.playSound("dwop");
                }
                return;
            }
            
            // Quick transfer from player to storage
            Item? playerItem = this.PlayerInventory.getItemAt(x, y);
            if (playerItem != null)
            {
                Game1.player.removeItemFromInventory(playerItem);
                
                if (Context.IsMainPlayer)
                {
                    // Similar to Left Click Logic: Check capacity via AddItem
                     ModEntry.Instance.Monitor.Log($"Right-click transfer: {playerItem.Name} x{playerItem.Stack}", LogLevel.Trace);
                     StorageManager.AddItem(this.SourceGuid, playerItem);
                     
                     var data = StorageManager.GetInventory(this.SourceGuid);
                     this.FullInventory = data.Inventory.Values.SelectMany(i => i).Cast<Item?>().ToList();
                     
                     // Re-filter manual
                    IEnumerable<Item?> items = this.FullInventory;
                    if (this.CurrentCategory != "All") items = items.Where(item => IsItemInCategory(item, this.CurrentCategory));
                    string query = this.SearchBar?.Text?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(query)) items = items.Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
                    this.FilteredInventory = items.ToList();
                }

                if (playerItem.Stack <= 0)
                {
                    // Fully moved
                    Game1.playSound("stoneStep");
                }
                else
                {
                    // Partially moved or rejected
                     Game1.playSound("cancel");
                     Game1.addHUDMessage(new HUDMessage("Storage Full", 3));
                     // Note: playerItem reference should be updated by AddItem logic because it's a reference type?
                     // Actually AddItem logic modifies the object. 
                     // But we should ensure logic consistency. 
                }
                
                this.RefreshView();
                return;
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

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            if (packet.SourceGuid != this.SourceGuid) return;
            
            var pageItems = packet.Items ?? new List<Item?>();
            this.StorageInventory.actualInventory = pageItems.Cast<Item>().ToList();
            this.IsLoading = false;
        }
    }
}
