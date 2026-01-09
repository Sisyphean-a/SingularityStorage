using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using SingularityStorage.Data;

namespace SingularityStorage.UI.Components
{
    public class CategorySidebar
    {
        public List<ClickableComponent> CategoryTabs { get; private set; } = new List<ClickableComponent>();
        public List<ClickableComponent> SubCategoryTabs { get; private set; } = new List<ClickableComponent>();
        
        public string SelectedGroup { get; private set; } = "全部";
        public int? SelectedSubCategory { get; private set; } // null 表示"全部"

        // 尺寸参数 - 确保能容纳四个中文字符
        private const int MajorTabWidth = 100;    // 主分类宽度（四字）
        private const int SubTabWidth = 88;       // 子分类宽度（三字为主）
        private const int TabHeight = 52;         // 标签高度
        private const int TabSpacing = 2;         // 标签之间的垂直间距
        private const int ColumnGap = 8;          // 两列之间的间距
        private const int TopPadding = 56;        // 顶部内边距，与主面板对齐
        
        private readonly List<string> _groups = CategoryData.Tabs;

        public event Action? OnFilterChanged;

        public void Initialize(int xPositionOnScreen, int yPositionOnScreen)
        {
            // 初始化分类标签 (主要分组)
            this.CategoryTabs.Clear();
            // 主分类列：在子分类列左侧
            var majorTabX = xPositionOnScreen - MajorTabWidth - SubTabWidth - ColumnGap - 12;
            var tabY = yPositionOnScreen + TopPadding;
            
            for (var i = 0; i < this._groups.Count; i++)
            {
                this.CategoryTabs.Add(new ClickableComponent(
                    new Rectangle(majorTabX, tabY + (i * (TabHeight + TabSpacing)), MajorTabWidth, TabHeight),
                    this._groups[i]));
            }
            
            this.UpdateSubCategories(xPositionOnScreen, yPositionOnScreen);
        }

        private void UpdateSubCategories(int xPositionOnScreen, int yPositionOnScreen)
        {
            this.SubCategoryTabs.Clear();
            // 子分类列：紧贴主面板左侧
            var subTabX = xPositionOnScreen - SubTabWidth - 4;
            var tabY = yPositionOnScreen + TopPadding;
            
            // 只有当选择了非"全部"的主分类时才显示子分类
            if (this.SelectedGroup == "全部")
            {
                return; // 不显示子分类
            }
            
            // 添加"全部" (All) 选项
            this.SubCategoryTabs.Add(new ClickableComponent(
                new Rectangle(subTabX, tabY, SubTabWidth, TabHeight),
                "全部")
            {
                myID = -9999 // "全部"选项的特殊 ID
            });
            
            if (CategoryData.CategoryGroups.TryGetValue(this.SelectedGroup, out var subCats))
            {
                 for (var i = 0; i < subCats.Count; i++)
                 {
                     var catId = subCats[i];
                     var name = CategoryData.GetCategoryName(catId);
                     
                     this.SubCategoryTabs.Add(new ClickableComponent(
                        new Rectangle(subTabX, tabY + ((i + 1) * (TabHeight + TabSpacing)), SubTabWidth, TabHeight),
                        name)
                     {
                         myID = catId
                     });
                 }
            }
        }

        public bool HandleClick(int x, int y, int xPositionOnScreen, int yPositionOnScreen)
        {
            // 处理主分类标签 (Major) 点击
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

            // 处理子分类标签点击
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
            // 获取当前鼠标位置，用于处理悬停高亮
            int mouseX = Game1.getOldMouseX();
            int mouseY = Game1.getOldMouseY();

            // ===========================
            // 绘制主标签 (Major Tabs)
            // ===========================
            foreach (var tab in this.CategoryTabs)
            {
                var isSelected = (tab.name == this.SelectedGroup);
                var isHovered = tab.containsPoint(mouseX, mouseY);

                // 1. 绘制背景 (使用扁平色块代替厚重边框)
                if (isSelected)
                {
                    // 选中状态：显示小麦色(Wheat)背景，透明度 60%
                    b.Draw(Game1.staminaRect, tab.bounds, Color.Wheat * 0.6f);
                }
                else if (isHovered)
                {
                    // 悬停状态：显示非常淡的背景，提供交互反馈
                    b.Draw(Game1.staminaRect, tab.bounds, Color.Wheat * 0.2f);
                }

                // 2. 确定文字颜色
                // 选中项用深褐色(标准文本色)，未选中项用带阴影的淡色，以此区分层级
                Color fontColor = isSelected ? Game1.textColor : Game1.textShadowColor;

                // 3. 计算文字居中位置
                var textSize = Game1.smallFont.MeasureString(tab.name);
                var textPos = new Vector2(
                    tab.bounds.X + (tab.bounds.Width - textSize.X) / 2,
                    tab.bounds.Y + (tab.bounds.Height - textSize.Y) / 2
                );

                // 4. 绘制文字 (使用带阴影的方法，让文字更清晰立体)
                Utility.drawTextWithShadow(b, tab.name, Game1.smallFont, textPos, fontColor);
            }

            // ===========================
            // 绘制子标签 (Sub Tabs)
            // ===========================
            if (this.SelectedGroup != "全部")
            {
                foreach (var tab in this.SubCategoryTabs)
                {
                    // 判断是否选中 (特殊处理 "全部" 的 ID: -9999)
                    var isSelected = (this.SelectedSubCategory == null && tab.myID == -9999) || 
                                    (this.SelectedSubCategory == tab.myID);
                    var isHovered = tab.containsPoint(mouseX, mouseY);

                    // 1. 绘制背景
                    if (isSelected)
                    {
                        b.Draw(Game1.staminaRect, tab.bounds, Color.Wheat * 0.6f);
                    }
                    else if (isHovered)
                    {
                        b.Draw(Game1.staminaRect, tab.bounds, Color.Wheat * 0.2f);
                    }

                    // 2. 确定文字颜色
                    Color fontColor = isSelected ? Game1.textColor : Game1.textShadowColor;

                    // 3. 计算文字居中位置
                    var textSize = Game1.smallFont.MeasureString(tab.name);
                    var textPos = new Vector2(
                        tab.bounds.X + (tab.bounds.Width - textSize.X) / 2,
                        tab.bounds.Y + (tab.bounds.Height - textSize.Y) / 2
                    );

                    // 4. 绘制文字
                    Utility.drawTextWithShadow(b, tab.name, Game1.smallFont, textPos, fontColor);
                }
            }
        }
    }
}
