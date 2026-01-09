using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace SingularityStorage.UI.Components
{
    public class ToolbarComponent
    {
        private TextBox? _searchBar;
        private ClickableTextureComponent? _okButton;
        private ClickableTextureComponent? _fillStacksButton;
        private ClickableTextureComponent? _storeAllButton;
        private string _lastSearchText = "";
        
        public event Action<string>? OnSearchChanged;
        public event Action? OnCloseClicked;
        public event Action? OnFillStacksClicked;
        public event Action? OnStoreAllClicked;

        // 公开访问器，以便菜单可以设置焦点
        public TextBox? SearchBar => _searchBar;
        public string SearchText => _searchBar?.Text ?? "";

        public void Initialize(int x, int y, int menuWidth, int menuHeight, MenuConfig config)
        {
            var headerY = y + config.Header.OffsetY;
            // 计算顶栏的垂直中心位置，用于按钮对齐
            var headerCenterY = headerY + (config.Header.Height - config.SearchBar.Height) / 2;

            // 搜索栏 - 垂直居中于顶栏
            this._searchBar = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                X = x + config.SearchBar.OffsetX,
                Y = headerCenterY,
                Width = config.SearchBar.Width,
                Height = config.SearchBar.Height
            };
            
            // 计算按钮的垂直位置，使其与顶栏垂直居中对齐
            var buttonCenterY = headerY + (config.Header.Height - config.FillStacksButton?.Size ?? 48) / 2;
            
            // 填充堆叠按钮 - 位于右侧
            if (config.FillStacksButton != null)
            {
                 var srcRect = new Rectangle(103, 469, 16, 16);
                 if (config.FillStacksButton.TextureSource != null)
                 {
                     srcRect = new Rectangle(
                         config.FillStacksButton.TextureSource.X,
                         config.FillStacksButton.TextureSource.Y,
                         config.FillStacksButton.TextureSource.Width,
                         config.FillStacksButton.TextureSource.Height);
                 }
                 
                 var scale = config.FillStacksButton.Size / (float)srcRect.Width;
                 
                 var buttonX = x + menuWidth - config.FillStacksButton.OffsetFromRight - config.FillStacksButton.Size;
                 var buttonY = headerY + (config.Header.Height - config.FillStacksButton.Size) / 2;
                 
                 this._fillStacksButton = new ClickableTextureComponent(
                    new Rectangle(
                        buttonX,
                        buttonY,
                        config.FillStacksButton.Size,
                        config.FillStacksButton.Size),
                    Game1.mouseCursors,
                    srcRect,
                    scale);
            }

            // 确认按钮 (OK)
            this._okButton = new ClickableTextureComponent(
                new Rectangle(
                    x + menuWidth - config.OkButton.OffsetFromRight,
                    y + menuHeight - config.OkButton.OffsetFromBottom,
                    config.OkButton.Size,
                    config.OkButton.Size),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f);

            // 全部存入按钮 - 位于填充按钮左侧
            if (config.StoreAllButton != null)
            {
                 var srcRect = new Rectangle(103, 469, 16, 16);
                 if (config.StoreAllButton.TextureSource != null)
                 {
                     srcRect = new Rectangle(
                         config.StoreAllButton.TextureSource.X,
                         config.StoreAllButton.TextureSource.Y,
                         config.StoreAllButton.TextureSource.Width,
                         config.StoreAllButton.TextureSource.Height);
                 }
                 
                 var scale = config.StoreAllButton.Size / (float)srcRect.Width;
                 
                 var buttonX = x + menuWidth - config.StoreAllButton.OffsetFromRight - config.StoreAllButton.Size;
                 var buttonY = headerY + (config.Header.Height - config.StoreAllButton.Size) / 2;
                 
                 this._storeAllButton = new ClickableTextureComponent(
                    new Rectangle(
                        buttonX,
                        buttonY,
                        config.StoreAllButton.Size,
                        config.StoreAllButton.Size),
                    Game1.mouseCursors,
                    srcRect,
                    scale);
            }
        }

        public void Update(GameTime time)
        {
             var query = this._searchBar?.Text?.Trim() ?? "";
             if (query != this._lastSearchText)
             {
                  this._lastSearchText = query;
                  OnSearchChanged?.Invoke(query);
             }
        }

        public bool HandleLeftClick(int x, int y)
        {
            if (this._okButton != null && this._okButton.containsPoint(x, y))
            {
                OnCloseClicked?.Invoke();
                return true;
            }

            if (this._fillStacksButton != null && this._fillStacksButton.containsPoint(x, y))
            {
                OnFillStacksClicked?.Invoke();
                return true;
            }

            if (this._storeAllButton != null && this._storeAllButton.containsPoint(x, y))
            {
                OnStoreAllClicked?.Invoke();
                return true;
            }
            
            if (this._searchBar != null)
            {
                this._searchBar.Update();
                // TextBox.Update handles click focus internally if we call SelectMe/Update, 
                // but usually we rely on Game1.keyboardDispatcher or manual selection.
                // Stardew's TextBox selection logic is a bit manual in menus.
                if (this._searchBarBounds(x, y))
                {
                    this._searchBar.SelectMe();
                }
                else
                {
                    this._searchBar.Selected = false; 
                    // Note: This might deselect if clicking elsewhere in the menu, which is standard behavior.
                }
            }

            return false;
        }

        private bool _searchBarBounds(int x, int y)
        {
            return _searchBar != null && 
                   x >= _searchBar.X && x <= _searchBar.X + _searchBar.Width && 
                   y >= _searchBar.Y && y <= _searchBar.Y + _searchBar.Height;
        }
        
        public void Draw(SpriteBatch b, MenuConfig config)
        {
            // 绘制搜索栏及占位符
            this._searchBar?.Draw(b);
            if (this._searchBar != null && string.IsNullOrEmpty(this._searchBar.Text) && !this._searchBar.Selected)
            {
                 b.DrawString(Game1.smallFont, config.SearchBar.Placeholder, new Vector2(this._searchBar.X + 10, this._searchBar.Y + 8), Color.Gray);
            }

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

            // 绘制"全部存入"按钮
            if (this._storeAllButton != null)
            {
                var isHovered = this._storeAllButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY());
                var bgColor = isHovered ? Color.Wheat : Color.White;
                
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                    this._storeAllButton.bounds.X, this._storeAllButton.bounds.Y,
                    this._storeAllButton.bounds.Width, this._storeAllButton.bounds.Height,
                    bgColor, 4f, false);
                
                var buttonText = "全存";
                var textSize = Game1.smallFont.MeasureString(buttonText);
                var textPos = new Vector2(
                    this._storeAllButton.bounds.X + (this._storeAllButton.bounds.Width - textSize.X) / 2,
                    this._storeAllButton.bounds.Y + (this._storeAllButton.bounds.Height - textSize.Y) / 2);
                
                Utility.drawTextWithShadow(b, buttonText, Game1.smallFont, textPos, Game1.textColor);
                
                if (isHovered)
                {
                    IClickableMenu.drawToolTip(b, "将背包中所有物品存入箱子", "全部存入", null);
                }
            }
        }

        public void PerformHover(int x, int y)
        {
            this._okButton?.tryHover(x, y);
            this._fillStacksButton?.tryHover(x, y);
            this._storeAllButton?.tryHover(x, y);
            this._searchBar?.Hover(x, y);
        }
    }
}
