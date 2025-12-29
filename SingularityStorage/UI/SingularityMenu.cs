using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SingularityStorage.Network;

namespace SingularityStorage.UI
{
    public class SingularityMenu : ItemGrabMenu
    {
        // Core Data
        private string SourceGuid;
        private List<Item?> FullInventory = new List<Item?>(); 
        private List<Item?> FilteredInventory = new List<Item?>();
        private int CurrentPage = 0;
        private const int ItemsPerPage = 36; 

        // UI Components
        private ClickableTextureComponent? NextPageButton;
        private ClickableTextureComponent? PrevPageButton;
        private TextBox? SearchBar;
        
        // State
        private string LastSearchText = "";
        private bool IsLoading = false;

        public SingularityMenu(string guid) : base(
            new List<Item?>(), 
            false, 
            true, 
            InventoryMenu.highlightAllItems, 
            null, 
            null, 
            null, 
            false, 
            true, 
            true, 
            true, 
            false, 
            0, 
            null, 
            -1, 
            null
        )
        {
            this.SourceGuid = guid;
            
            this.SetupWidgets();

            if (Context.IsMainPlayer)
            {
                // Host: Load directly
                var data = StorageManager.GetInventory(guid);
                this.FullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                this.FilteredInventory = this.FullInventory;
                this.RefreshView();
            }
            else
            {
                // Client: Request data
                this.IsLoading = true;
                NetworkManager.SendRequestView(guid, 0, "");
                // Initial view is empty until packet arrives
                this.ItemsToGrabMenu.actualInventory = new List<Item>();
            }

            this.behaviorOnItemGrab = this.OnItemGrabbed;
            this.ItemsToGrabMenu.highlightMethod = this.HighlightItem;
        }

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            if (packet.SourceGuid != this.SourceGuid) return;
            
            // On client, we just blindly trust the page sent by Host
            var pageItems = packet.Items ?? new List<Item?>();
            
            // Pad
            while (pageItems.Count < ItemsPerPage)
            {
                pageItems.Add(null);
            }
            
            this.ItemsToGrabMenu.actualInventory = pageItems;
            this.IsLoading = false;
        }
        
        private void SetupWidgets()
        {
            int x = this.ItemsToGrabMenu.xPositionOnScreen;
            int y = this.ItemsToGrabMenu.yPositionOnScreen - 64; 

            this.SearchBar = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
            {
                X = x + 128,
                Y = y - 10,
                Width = 400
            };

            this.PrevPageButton = new ClickableTextureComponent(
                new Rectangle(x - 64, y, 48, 44), 
                Game1.mouseCursors, 
                new Rectangle(352, 495, 12, 11), 
                4f);

            this.NextPageButton = new ClickableTextureComponent(
                new Rectangle(x + this.ItemsToGrabMenu.width + 16, y, 48, 44), 
                Game1.mouseCursors, 
                new Rectangle(365, 495, 12, 11), 
                4f);
        }

        private void RefreshView()
        {
            if (!Context.IsMainPlayer)
            {
                // Client: Request new page
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
            
            while (pageItems.Count < ItemsPerPage)
            {
                pageItems.Add(null);
            }

            this.ItemsToGrabMenu.actualInventory = pageItems;
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
                        .Where(item => item != null && item.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }
            }
            
            this.RefreshView();
        }

        private void OnItemGrabbed(Item item, Farmer who)
        {
            if (Context.IsMainPlayer)
            {
                this.FullInventory.RemoveAll(i => i == null || i.Stack <= 0);
                
                foreach (var pageItem in this.ItemsToGrabMenu.actualInventory)
                {
                    if (pageItem != null && !this.FullInventory.Contains(pageItem))
                    {
                        StorageManager.AddItem(this.SourceGuid, pageItem);
                        if (!this.FullInventory.Contains(pageItem)) 
                             this.FullInventory.Add(pageItem);
                    }
                }
                
                this.FullInventory.RemoveAll(i => i == null || i.Stack <= 0);
            }
            else
            {
                // Client: Send Transfer Request (TODO)
                // For now, client modification is tricky without packets.
                // We should implement PacketType.RequestTransfer
            }
        }
        
        private bool HighlightItem(Item item)
        {
             return true; 
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
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
                 // For client, we might not know total pages unless host sent it. 
                 // Assuming infinite forward for now or packet.TotalItems logic.
                 this.CurrentPage++;
                 Game1.playSound("shwip");
                 this.RefreshView();
                 return;
            }
            
            if (this.SearchBar != null && this.SearchBar.Selected)
            {
                // Pass click to search bar
            }
            
            base.receiveLeftClick(x, y, playSound);
            
            if (this.SearchBar != null)
            {
                this.SearchBar.Update();
                this.SearchBar.SelectMe(); 
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            this.UpdateSearch();
        }

        public override void draw(SpriteBatch b)
        {
             b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
             base.draw(b);
             
             this.SearchBar?.Draw(b);
             this.PrevPageButton?.draw(b);
             this.NextPageButton?.draw(b);
             
             if (this.PrevPageButton != null)
             {
                 string pageText = $"Page {this.CurrentPage + 1}";
                 Utility.drawTextWithShadow(b, pageText, Game1.smallFont, new Vector2(this.PrevPageButton.bounds.Right + 16, this.PrevPageButton.bounds.Y + 12), Game1.textColor);
             }
                 
             if (this.IsLoading)
             {
                 Utility.drawTextWithShadow(b, "Loading...", Game1.smallFont, new Vector2(this.ItemsToGrabMenu.xPositionOnScreen, this.ItemsToGrabMenu.yPositionOnScreen - 32), Color.Yellow);
             }

             this.drawMouse(b);
        }
    }
}
