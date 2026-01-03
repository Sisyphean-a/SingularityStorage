using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SingularityStorage.UI.Components
{
    public class CategorySidebar
    {
        public List<ClickableComponent> CategoryTabs { get; private set; } = new List<ClickableComponent>();
        public List<ClickableComponent> SubCategoryTabs { get; private set; } = new List<ClickableComponent>();
        
        public string SelectedGroup { get; private set; } = "全部";
        public int? SelectedSubCategory { get; private set; } // null means 'All'

        private const int TabWidth = 110;
        private const int TabHeight = 64;
        private readonly List<string> _groups = Data.CategoryData.Tabs;

        public event Action? OnFilterChanged;

        public void Initialize(int xPositionOnScreen, int yPositionOnScreen)
        {
            // Initialize Category Tabs (Major Groups)
            this.CategoryTabs.Clear();
            var majorTabX = xPositionOnScreen - (TabWidth * 2) - 8; // Extra padding
            var tabY = yPositionOnScreen + 64;
            
            for (var i = 0; i < this._groups.Count; i++)
            {
                this.CategoryTabs.Add(new ClickableComponent(
                    new Rectangle(majorTabX, tabY + (i * TabHeight), TabWidth, TabHeight), 
                    this._groups[i]));
            }
            
            this.UpdateSubCategories(xPositionOnScreen, yPositionOnScreen);
        }

        private void UpdateSubCategories(int xPositionOnScreen, int yPositionOnScreen)
        {
            this.SubCategoryTabs.Clear();
            var subTabX = xPositionOnScreen - TabWidth;
            var tabY = yPositionOnScreen + 64;
            
            // Always add "All" (全部) option for sub-category
            this.SubCategoryTabs.Add(new ClickableComponent(
                new Rectangle(subTabX, tabY, TabWidth, TabHeight), 
                "全部") 
            { 
                myID = -9999 // Special ID for 'All'
            });
            
            if (Data.CategoryData.CategoryGroups.TryGetValue(this.SelectedGroup, out var subCats))
            {
                 for (var i = 0; i < subCats.Count; i++)
                 {
                     var catId = subCats[i];
                     var name = Data.CategoryData.GetCategoryName(catId);
                     
                     this.SubCategoryTabs.Add(new ClickableComponent(
                        new Rectangle(subTabX, tabY + ((i + 1) * TabHeight), TabWidth, TabHeight), 
                        name)
                     {
                         myID = catId
                     });
                 }
            }
        }

        public bool HandleClick(int x, int y, int xPositionOnScreen, int yPositionOnScreen)
        {
            // Handle Category Tabs (Major)
            foreach (var tab in this.CategoryTabs)
            {
                if (tab.containsPoint(x, y))
                {
                    if (this.SelectedGroup != tab.name)
                    {
                        this.SelectedGroup = tab.name;
                        this.SelectedSubCategory = null; 
                        this.UpdateSubCategories(xPositionOnScreen, yPositionOnScreen);
                        this.OnFilterChanged?.Invoke();
                        Game1.playSound("smallSelect");
                    }
                    return true;
                }
            }

            // Handle SubCategory Tabs
            if (this.SelectedGroup != "全部")
            {
                foreach (var tab in this.SubCategoryTabs)
                {
                    if (tab.containsPoint(x, y))
                    {
                        var newSub = tab.myID;
                        var resolvedCurrent = this.SelectedSubCategory ?? -9999;
                        
                        if (resolvedCurrent != newSub)
                        {
                            if (newSub == -9999) this.SelectedSubCategory = null;
                            else this.SelectedSubCategory = newSub;
                            
                            this.OnFilterChanged?.Invoke();
                            Game1.playSound("smallSelect");
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public void Draw(SpriteBatch b)
        {
            // Draw Major Tabs
            foreach (var tab in this.CategoryTabs)
            {
                 var selected = (tab.name == this.SelectedGroup);
                 IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(16, 368, 16, 16), 
                     tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, 
                     selected ? Color.White : Color.Gray, 4f, false);
                 
                 var textSize = Game1.smallFont.MeasureString(tab.name);
                 var textPos = new Vector2(
                    tab.bounds.X + (tab.bounds.Width - textSize.X) / 2, 
                    tab.bounds.Y + (tab.bounds.Height - textSize.Y) / 2 + 4);
                 b.DrawString(Game1.smallFont, tab.name, textPos, Game1.textColor);
            }
            
            // Draw Sub Tabs
            if (this.SelectedGroup != "全部")
            {
                foreach (var tab in this.SubCategoryTabs)
                {
                     var selected = (this.SelectedSubCategory == null && tab.myID == -9999) || (this.SelectedSubCategory == tab.myID);
                     IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(16, 368, 16, 16), 
                         tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, 
                         selected ? Color.White : Color.Gray * 0.9f, 4f, false);
                         
                     var textSize = Game1.smallFont.MeasureString(tab.name);
                     var textPos = new Vector2(
                        tab.bounds.X + (tab.bounds.Width - textSize.X) / 2, 
                        tab.bounds.Y + (tab.bounds.Height - textSize.Y) / 2 + 4);
                     b.DrawString(Game1.smallFont, tab.name, textPos, Game1.textColor);
                }
            }
        }
    }
}
