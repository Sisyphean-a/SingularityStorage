using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SingularityStorage.Network;
using SingularityStorage.UI.Components;
using SingularityStorage.UI.Controllers;

namespace SingularityStorage.UI
{
    public class SingularityMenu : IClickableMenu
    {
        // 配置
        private static MenuConfig? _config;
        private static MenuConfig Config
        {
            get
            {
                if (_config != null) return _config;
                var configPath = Path.Combine(ModEntry.Instance!.Helper.DirectoryPath, "UI", "MenuConfig.json");
                _config = MenuConfig.Load(configPath);
                return _config;
            }
        }

        // 核心数据
        private string _sourceGuid;
        private List<Item?> _fullInventory = new List<Item?>(); 
        private List<Item?> _filteredInventory = new List<Item?>();
        private int ItemsPerPage => Config.StorageInventory.Columns * Config.StorageInventory.Rows;
        
        private string _cachedCapacityText = "";
        
        // 组件
        private CategorySidebar _sidebar;
        private PaginationControl _pagination;
        private InventoryHandler _inventoryHandler;

        // UI 控件
        private InventoryMenu _storageInventory;
        private InventoryMenu _playerInventory;
        private TextBox? _searchBar;
        private ClickableTextureComponent? _okButton;
        private ClickableTextureComponent? _fillStacksButton;
        
        // 状态
        private string _lastSearchText = "";
        private bool _isLoading;
        private Item? _hoverItem;

        public SingularityMenu(string guid)
        {
            this._sourceGuid = guid;

            // 从配置加载尺寸信息
            this.width = Config.MenuDimensions.Width;
            this.height = Config.MenuDimensions.Height;
            this.xPositionOnScreen = (Game1.uiViewport.Width - this.width) / 2;
            this.yPositionOnScreen = (Game1.uiViewport.Height - this.height) / 2;

            // 从配置初始化存储库存界面
            this._storageInventory = new InventoryMenu(
                this.xPositionOnScreen + Config.StorageInventory.OffsetX,
                this.yPositionOnScreen + Config.StorageInventory.OffsetY,
                false,
                new List<Item>(),
                null,
                ItemsPerPage,
                Config.StorageInventory.Rows,
                Config.StorageInventory.SlotSpacing,
                Config.StorageInventory.SlotSpacing
            );

            // 从配置初始化玩家库存界面
            this._playerInventory = new InventoryMenu(
                this.xPositionOnScreen + Config.PlayerInventory.OffsetX,
                this.yPositionOnScreen + this.height - Config.PlayerInventory.OffsetFromBottom,
                true
            );

            // 初始化组件
            this._sidebar = new CategorySidebar();
            this._pagination = new PaginationControl(ItemsPerPage);
            this._inventoryHandler = new InventoryHandler(
                guid, 
                this._storageInventory, 
                this._playerInventory,
                () => this._fullInventory, 
                () => this.ApplyFilters() // 刷新回调，调用 ApplyFilters 以进行重新排序/过滤
            );
            
            // 绑定事件
            this._sidebar.OnFilterChanged += this.ApplyFilters;
            this._pagination.OnPageChanged += this.RefreshView;

            this.InitializeWidgets();

            if (Context.IsMainPlayer)
            {
                var data = StorageManager.GetInventory(guid);
                this._fullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
                this._filteredInventory = this._fullInventory;
                this.RefreshView();
            }
            else
            {
                this._isLoading = true;
                NetworkManager.SendRequestView(guid, 0, "");
            }
        }

        private void InitializeWidgets()
        {
            var headerY = this.yPositionOnScreen + Config.Header.OffsetY;

            // 搜索栏
            this._searchBar = new TextBox(
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

            // 组件初始化
            this._sidebar.Initialize(this.xPositionOnScreen, this.yPositionOnScreen);
            this._pagination.Initialize(this.xPositionOnScreen, this.yPositionOnScreen, Config);

            // 填充堆叠按钮
            if (Config.FillStacksButton != null)
            {
                 var srcRect = new Rectangle(103, 469, 16, 16);
                 if (Config.FillStacksButton.TextureSource != null)
                 {
                     srcRect = new Rectangle(
                         Config.FillStacksButton.TextureSource.X, 
                         Config.FillStacksButton.TextureSource.Y, 
                         Config.FillStacksButton.TextureSource.Width, 
                         Config.FillStacksButton.TextureSource.Height);
                 }
                 
                 var scale = Config.FillStacksButton.Size / (float)srcRect.Width;
                 
                 var buttonX = this.xPositionOnScreen + this.width - Config.FillStacksButton.OffsetFromRight - Config.FillStacksButton.Size;
                 var buttonY = this.yPositionOnScreen + Config.Header.OffsetY + 8; 
                 
                 this._fillStacksButton = new ClickableTextureComponent(
                    new Rectangle(
                        buttonX,
                        buttonY,
                        Config.FillStacksButton.Size,
                        Config.FillStacksButton.Size),
                    Game1.mouseCursors,
                    srcRect,
                    scale);
            }

            // 确认按钮 (OK)
            this._okButton = new ClickableTextureComponent(
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
                this._isLoading = true;
                NetworkManager.SendRequestView(this._sourceGuid, this._pagination.CurrentPage, this._searchBar?.Text ?? "");
                return;
            }

            // 主要是为了确保同步而从源重新加载数据，
            // 尽管我们通常已经更新了 FullInventory。
            // 为了万无一失：
            var data = StorageManager.GetInventory(this._sourceGuid);
            this._fullInventory = data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList();
            
            // 重新应用过滤逻辑（尽可能不重置页面，但 ApplyFilters 通常会重置页面）
            // 这里我们只想根据当前的 FilteredInventory 和 Page 更新视图。
            // 但是如果 FullInventory 发生了变化，我们可能需要重新过滤。
            // 假设 FilteredInventory 相对于 FullInventory 是最新的，除非我们刚刚添加/删除了物品。
            // 实际上，InventoryHandler 更新存储并调用此方法。
            // 所以我们确实需要重新过滤，调用 UpdateFilteredList 逻辑。
            
            // 但是 ApplyFilters 会将 Page 重置为 0。这对于点击“下一页”来说是不利的。
            // 所以我们将“更新列表”与“重置过滤器”分开。
            
            this.UpdateFilteredList();

            var totalItems = this._filteredInventory.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)ItemsPerPage);
            if (totalPages == 0) totalPages = 1;

            if (this._pagination.CurrentPage >= totalPages) this._pagination.ResetPage(); // 或者设置为最大值
            
            var startIndex = this._pagination.CurrentPage * ItemsPerPage;
            var pageItems = this._filteredInventory
                .Skip(startIndex)
                .Take(ItemsPerPage)
                .ToList();

            this._storageInventory.actualInventory = pageItems.Cast<Item>().ToList();
            
            var (used, max) = StorageManager.GetCounts(this._sourceGuid);
            this._cachedCapacityText = $"{used} / {max}";
        }

        private void UpdateFilteredList()
        {
             var query = this._searchBar?.Text?.Trim() ?? "";
             
             IEnumerable<Item?> items = this._fullInventory;

             // 1. 按分类组过滤
             if (this._sidebar.SelectedGroup != "全部")
             {
                 items = items.Where(item => Data.CategoryData.IsItemInGroup(item, this._sidebar.SelectedGroup));
                 
                 // 2. 按子分类过滤
                 if (this._sidebar.SelectedSubCategory.HasValue && this._sidebar.SelectedSubCategory.Value != -9999)
                 {
                     items = items.Where(item => item != null && item.Category == this._sidebar.SelectedSubCategory.Value);
                 }
             }

             // 3. 按搜索关键词过滤
             if (!string.IsNullOrEmpty(query))
             {
                 items = items.Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
             }

             this._filteredInventory = items.ToList();
        }

        private void ApplyFilters()
        {
            this.UpdateFilteredList();
            this._pagination.ResetPage();
            this.RefreshView();
        }

        private void UpdateSearch()
        {
            var query = this._searchBar?.Text?.Trim() ?? "";
            if (query != this._lastSearchText)
            {
                 this._lastSearchText = query;
                 this.ApplyFilters();
            }
        }
        
        private void FillExistingStacks()
        {
             // 与原始逻辑类似，逻辑可以移至 Handler，但暂时保留在此处，因为它负责编排。
             // Handler 处理单次点击。
             var playerItems = Game1.player.Items.Where(i => i != null).ToList();
             var changed = false;

             foreach (var pItem in playerItems.OfType<Item>()
                          .Select(pItem => new
                          {
                              pItem,
                              exists = _fullInventory.Any(sItem => sItem != null && sItem.canStackWith(pItem))
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
             RefreshView();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            // 控件点击处理
            if (this._okButton != null && this._okButton.containsPoint(x, y))
            {
                this.exitThisMenu();
                return;
            }

            if (this._fillStacksButton != null && this._fillStacksButton.containsPoint(x, y))
            {
                this.FillExistingStacks();
                return;
            }
            
            // 组件点击处理
            if (this._pagination.HandleClick(x, y, this._filteredInventory.Count)) return;
            if (this._sidebar.HandleClick(x, y, this.xPositionOnScreen, this.yPositionOnScreen)) return;

            var isShift = (Game1.oldKBState.IsKeyDown(Keys.LeftShift) || Game1.oldKBState.IsKeyDown(Keys.RightShift));
            
            // 库存处理器 (Inventory Handler) 点击处理
            this._inventoryHandler.HandleLeftClick(x, y, isShift);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (this._pagination.HandleClick(x, y, this._filteredInventory.Count)) return; // 将分页按钮上的右键点击视为普通点击？通常不会，但很安全。

            if (this._inventoryHandler.HandleRightClick(x, y)) return;
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            
            this._hoverItem = this._inventoryHandler.CheckForHover(x, y);

            this._pagination.PerformHover(x, y);
            
            this._okButton?.tryHover(x, y);
            this._fillStacksButton?.tryHover(x, y);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            this.UpdateSearch();
        }

        public override void draw(SpriteBatch b)
        {
            // 绘制渐变背景叠加层
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

            // 绘制侧边栏分类
            this._sidebar.Draw(b);
            
            // 绘制主菜单对话框背景
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

            // 绘制标题
            var title = Config.Title.Text;
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, 
                new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + Config.Title.OffsetY), 
                Game1.textColor);

            // 绘制顶栏背景
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                this.xPositionOnScreen + Config.Header.Padding, 
                this.yPositionOnScreen + Config.Header.OffsetY, 
                this.width - (Config.Header.Padding * 2), 
                Config.Header.Height, 
                Color.White, 4f, false);

            // 绘制控件
            this._searchBar?.Draw(b);
            if (this._searchBar != null && string.IsNullOrEmpty(this._searchBar.Text) && !this._searchBar.Selected)
            {
                 b.DrawString(Game1.smallFont, Config.SearchBar.Placeholder, new Vector2(this._searchBar.X + 10, this._searchBar.Y + 8), Color.Gray);
            }

            // 绘制分页控制
            this._pagination.Draw(b, this._filteredInventory.Count);

            // 绘制仓库库存
            this._storageInventory.draw(b);

            // 绘制分隔线
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                this.xPositionOnScreen + Config.Header.Padding, 
                this._playerInventory.yPositionOnScreen - Config.Separator.OffsetFromInventory, 
                this.width - (Config.Header.Padding * 2), 
                Config.Separator.Height, 
                Color.White, 4f, false);

            // 绘制玩家背包库存
            this._playerInventory.draw(b);

            // 绘制 OK 按钮
            this._okButton?.draw(b);

            // 绘制“填充堆叠”按钮
            if (this._fillStacksButton != null)
            {
                var isHovered = this._fillStacksButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY());
                var bgColor = isHovered ? Color.Wheat : Color.White;
                
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                    this._fillStacksButton.bounds.X, this._fillStacksButton.bounds.Y,
                    this._fillStacksButton.bounds.Width, this._fillStacksButton.bounds.Height,
                    bgColor, 4f, false);
                
                var buttonText = "填充";
                var textSize = Game1.smallFont.MeasureString(buttonText);
                var textPos = new Vector2(
                    this._fillStacksButton.bounds.X + (this._fillStacksButton.bounds.Width - textSize.X) / 2,
                    this._fillStacksButton.bounds.Y + (this._fillStacksButton.bounds.Height - textSize.Y) / 2);
                
                Utility.drawTextWithShadow(b, buttonText, Game1.smallFont, textPos, Game1.textColor);
                
                if (isHovered)
                {
                    IClickableMenu.drawToolTip(b, "将背包中已存在于箱子的物品全部存入", "填充堆叠", null);
                }
            }

            // Draw Capacity
            if (!string.IsNullOrEmpty(this._cachedCapacityText))
            {
                 var capSize = Game1.smallFont.MeasureString(this._cachedCapacityText);
                 Utility.drawTextWithShadow(b, this._cachedCapacityText, Game1.smallFont,
                     new Vector2(this._storageInventory.xPositionOnScreen + this._storageInventory.width - capSize.X, 
                                 this._storageInventory.yPositionOnScreen + this._storageInventory.height + 4), 
                     Color.White);
            }

            // 绘制拿在手中的物品
            if (this._inventoryHandler.HeldItem != null)
            {
                this._inventoryHandler.HeldItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
            }

            // 绘制悬停文本
            if (this._hoverItem != null && this._inventoryHandler.HeldItem == null)
            {
                IClickableMenu.drawToolTip(b, this._hoverItem.getDescription(), this._hoverItem.DisplayName, this._hoverItem);
            }

            // 绘制加载中的提示文本
            if (this._isLoading)
            {
                Utility.drawTextWithShadow(b, Config.LoadingText.Text, Game1.smallFont,
                    new Vector2(this._storageInventory.xPositionOnScreen, this._storageInventory.yPositionOnScreen + Config.LoadingText.OffsetY), Color.Yellow);
            }

            // 绘制鼠标指针
            this.drawMouse(b);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            
            this.xPositionOnScreen = (Game1.uiViewport.Width - Config.MenuDimensions.Width) / 2;
            this.yPositionOnScreen = (Game1.uiViewport.Height - Config.MenuDimensions.Height) / 2;
            
            this.InitializeWidgets();
        }

        protected override void cleanupBeforeExit()
        {
            this._inventoryHandler.ReturnHeldItem();
            base.cleanupBeforeExit();
        }

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            if (packet.SourceGuid != this._sourceGuid) return;
            
            var pageItems = packet.Items ?? new List<Item?>();
            this._storageInventory.actualInventory = pageItems.Cast<Item>().ToList();
            this._isLoading = false;
        }
    }
}
