using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SingularityStorage.UI.Components
{
    public class PaginationControl
    {
        public int CurrentPage { get; private set; }
        private int _itemsPerPage;
        
        private ClickableTextureComponent? _nextPageButton;
        private ClickableTextureComponent? _prevPageButton;

        public event Action? OnPageChanged;

        public PaginationControl(int itemsPerPage)
        {
            this._itemsPerPage = itemsPerPage;
        }

        public void Initialize(int xPositionOnScreen, int yPositionOnScreen, MenuConfig config)
        {
            var headerY = yPositionOnScreen + config.Header.OffsetY;

            this._prevPageButton = new ClickableTextureComponent(
                new Rectangle(
                    xPositionOnScreen + config.PageButtons.PrevOffsetX,
                    headerY,
                    config.PageButtons.Width,
                    config.PageButtons.Height),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                4f);

            this._nextPageButton = new ClickableTextureComponent(
                new Rectangle(
                    xPositionOnScreen + config.PageButtons.NextOffsetX,
                    headerY,
                    config.PageButtons.Width,
                    config.PageButtons.Height),
                Game1.mouseCursors,
                new Rectangle(365, 495, 12, 11),
                4f);
        }

        public void ResetPage()
        {
            this.CurrentPage = 0;
        }

        public void HandlePageChange(int direction, int totalItems)
        {
            var totalPages = (int)Math.Ceiling(totalItems / (double)this._itemsPerPage);
            if (totalPages == 0) totalPages = 1;

            if (direction < 0 && this.CurrentPage > 0)
            {
                this.CurrentPage--;
                Game1.playSound("shwip");
                this.OnPageChanged?.Invoke();
            }
            else if (direction > 0 && this.CurrentPage < totalPages - 1)
            {
                this.CurrentPage++;
                Game1.playSound("shwip");
                this.OnPageChanged?.Invoke();
            }
        }
        
        public bool HandleClick(int x, int y, int totalItems)
        {
            if (this._prevPageButton != null && this._prevPageButton.containsPoint(x, y))
            {
                this.HandlePageChange(-1, totalItems);
                return true;
            }

            if (this._nextPageButton != null && this._nextPageButton.containsPoint(x, y))
            {
                this.HandlePageChange(1, totalItems);
                return true;
            }
            return false;
        }

        public void PerformHover(int x, int y)
        {
            this._prevPageButton?.tryHover(x, y);
            this._nextPageButton?.tryHover(x, y);
        }

        public void Draw(SpriteBatch b, int totalItems)
        {
            this._prevPageButton?.draw(b);
            this._nextPageButton?.draw(b);

            // 绘制页码
            if (this._prevPageButton != null && this._nextPageButton != null)
            {
                var totalPages = (int)Math.Ceiling(totalItems / (double)this._itemsPerPage);
                if (totalPages == 0) totalPages = 1;
                
                var pageText = $"{this.CurrentPage + 1}/{totalPages}";
                var textSize = Game1.smallFont.MeasureString(pageText);
                var btnCenter = (this._prevPageButton.bounds.Right + this._nextPageButton.bounds.Left) / 2f;
                
                Utility.drawTextWithShadow(b, pageText, Game1.smallFont,
                    new Vector2(btnCenter - textSize.X / 2, this._prevPageButton.bounds.Y + 12), Game1.textColor);
            }
        }
    }
}
