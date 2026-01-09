using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SingularityStorage.Network;
using SingularityStorage.UI.Components;
using SingularityStorage.UI.Controllers;
using SingularityStorage.UI.Data;

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
        private InventoryDataSource _dataSource = new InventoryDataSource();
        private int ItemsPerPage => Config.StorageInventory.Columns * Config.StorageInventory.Rows;
        
        private string _cachedCapacityText = "";
        
        private ToolbarComponent _toolbar;
        
        // 组件
        private CategorySidebar _sidebar;
        private PaginationControl _pagination;
        private InventoryHandler _inventoryHandler;
        
        // UI 控件
        private InventoryMenu _storageInventory;
        private InventoryMenu _playerInventory;
        
        // 状态
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
            this._toolbar = new ToolbarComponent();
            
            this._inventoryHandler = new InventoryHandler(
                guid, 
                this._storageInventory, 
                this._playerInventory,
                () => this._dataSource.FullItems.ToList(), 
                () => this.ApplyFilters() // 刷新回调，调用 ApplyFilters 以进行重新排序/过滤
            );
            
            // 绑定事件
            this._sidebar.OnFilterChanged += this.ApplyFilters;
            this._pagination.OnPageChanged += this.RefreshView;
            
            this._toolbar.OnSearchChanged += (text) => this.ApplyFilters();
            this._toolbar.OnCloseClicked += this.exitThisMenu;
            this._toolbar.OnFillStacksClicked += this.FillExistingStacks;
            this._toolbar.OnStoreAllClicked += this.StoreAll;

            this.InitializeWidgets();

            if (Context.IsMainPlayer)
            {
                var data = StorageManager.GetInventory(guid);
                this._dataSource.UpdateSource(data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList());
                this._dataSource.ApplyFilter("", "全部", null); 
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
            this._sidebar.Initialize(this.xPositionOnScreen, this.yPositionOnScreen);
            this._pagination.Initialize(this.xPositionOnScreen, this.yPositionOnScreen, Config);
            this._toolbar.Initialize(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Config);
        }

        private void RefreshView()
        {
            if (!Context.IsMainPlayer)
            {
                this._isLoading = true;
                NetworkManager.SendRequestView(this._sourceGuid, this._pagination.CurrentPage, this._toolbar.SearchText);
                return;
            }

            // 主要是为了确保同步而从源重新加载数据
            var data = StorageManager.GetInventory(this._sourceGuid);
            this._dataSource.UpdateSource(data.Inventory.Values.SelectMany(x => x).Cast<Item?>().ToList());
            
            // 重新应用过滤逻辑
            var query = this._toolbar.SearchText;
            this._dataSource.ApplyFilter(query, this._sidebar.SelectedGroup, this._sidebar.SelectedSubCategory);
            
            var totalItems = this._dataSource.TotalCount;
            var totalPages = this._dataSource.GetTotalPages(ItemsPerPage);

            if (this._pagination.CurrentPage >= totalPages) this._pagination.ResetPage();
            
            var pageItems = this._dataSource.GetPage(this._pagination.CurrentPage, ItemsPerPage);
            this._storageInventory.actualInventory = pageItems;
            
            var (used, max) = StorageManager.GetCounts(this._sourceGuid);
            this._cachedCapacityText = $"{used} / {max}";
        }

        private void ApplyFilters()
        {
            var query = this._toolbar.SearchText;
            this._dataSource.ApplyFilter(query, this._sidebar.SelectedGroup, this._sidebar.SelectedSubCategory);
            this._pagination.ResetPage();
            this.RefreshView();
        }

        // UpdateSearch Removed (Logic moved to ToolbarComponent)
        
        private void FillExistingStacks()
        {
             this._inventoryHandler.FillExistingStacks();
        }

        private void StoreAll()
        {
             this._inventoryHandler.StoreAll();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            // 控件点击处理
            if (this._toolbar.HandleLeftClick(x, y)) return;
            
            // 组件点击处理
            if (this._pagination.HandleClick(x, y, this._dataSource.TotalCount)) return;
            if (this._sidebar.HandleClick(x, y, this.xPositionOnScreen, this.yPositionOnScreen)) return;

            var isShift = (Game1.oldKBState.IsKeyDown(Keys.LeftShift) || Game1.oldKBState.IsKeyDown(Keys.RightShift));
            
            // 库存处理器 (Inventory Handler) 点击处理
            this._inventoryHandler.HandleLeftClick(x, y, isShift);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (this._pagination.HandleClick(x, y, this._dataSource.TotalCount)) return; // 将分页按钮上的右键点击视为普通点击？通常不会，但很安全。

            if (this._inventoryHandler.HandleRightClick(x, y)) return;
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            
            this._hoverItem = this._inventoryHandler.CheckForHover(x, y);

            this._pagination.PerformHover(x, y);
            this._toolbar.PerformHover(x, y);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            this._toolbar.Update(time);
        }

        public override void draw(SpriteBatch b)
        {
            // 绘制渐变背景叠加层
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

            // 绘制侧边栏分类
            this._sidebar.Draw(b);
            
            // 绘制主菜单对话框背景
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

            // 绘制顶栏背景
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                this.xPositionOnScreen + Config.Header.Padding, 
                this.yPositionOnScreen + Config.Header.OffsetY, 
                this.width - (Config.Header.Padding * 2), 
                Config.Header.Height, 
                Color.White, 4f, false);

            // 绘制工具栏 (搜索框, 按钮)
            this._toolbar.Draw(b, Config);
            
            // 绘制分页控制
            this._pagination.Draw(b, this._dataSource.TotalCount);

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
