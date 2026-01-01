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
        }

        private void UpdateSearch()
        {
            string query = this.SearchBar?.Text?.Trim() ?? "";
            
            if (query == this.LastSearchText) return;
            
            this.LastSearchText = query;
            this.CurrentPage = 0;

            if (Context.IsMainPlayer)
            {
                if (string.IsNullOrEmpty(query))
                {
                    this.FilteredInventory = this.FullInventory;
                }
                else
                {
                    this.FilteredInventory = this.FullInventory
                        .Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            
            this.RefreshView();
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
                this.CurrentPage++;
                Game1.playSound("shwip");
                this.RefreshView();
                return;
            }

            if (this.OkButton != null && this.OkButton.containsPoint(x, y))
            {
                this.exitThisMenu();
                return;
            }

            // Handle Storage Inventory clicks
            Item? clickedItem = this.StorageInventory.getItemAt(x, y);
            if (clickedItem != null && this.HeldItem == null)
            {
                this.HeldItem = clickedItem;
                this.StorageInventory.actualInventory.Remove(clickedItem);
                this.FullInventory.Remove(clickedItem);
                this.FilteredInventory.Remove(clickedItem);
                
                // Sync with StorageManager
                if (Context.IsMainPlayer)
                {
                    StorageManager.RemoveItem(this.SourceGuid, clickedItem);
                }
                
                Game1.playSound("dwop");
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
                        StorageManager.AddItem(this.SourceGuid, this.HeldItem);
                        
                        // Reload data from StorageManager to avoid duplicates
                        var data = StorageManager.GetInventory(this.SourceGuid);
                        this.FullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                        this.FilteredInventory = this.FullInventory;
                    }
                    this.RefreshView();
                    this.HeldItem = null;
                    Game1.playSound("stoneStep");
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
                    Game1.player.addItemToInventory(storageItem);
                    this.StorageInventory.actualInventory.Remove(storageItem);
                    this.FullInventory.Remove(storageItem);
                    this.FilteredInventory.Remove(storageItem);
                    
                    if (Context.IsMainPlayer)
                    {
                        StorageManager.RemoveItem(this.SourceGuid, storageItem);
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
                    StorageManager.AddItem(this.SourceGuid, playerItem);
                    
                    // Reload data from StorageManager to avoid duplicates
                    var data = StorageManager.GetInventory(this.SourceGuid);
                    this.FullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                    this.FilteredInventory = this.FullInventory;
                }
                this.RefreshView();
                Game1.playSound("stoneStep");
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
