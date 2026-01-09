using Newtonsoft.Json;

namespace SingularityStorage.UI
{
    /// <summary>
    /// 奇点菜单 UI 的配置数据。
    /// </summary>
    public class MenuConfig
    {
        public MenuDimensions MenuDimensions { get; set; } = new();
        public TitleConfig Title { get; set; } = new();
        public HeaderConfig Header { get; set; } = new();
        public SearchBarConfig SearchBar { get; set; } = new();
        public PageButtonsConfig PageButtons { get; set; } = new();
        public InventoryConfig StorageInventory { get; set; } = new();
        public PlayerInventoryConfig PlayerInventory { get; set; } = new();
        public SeparatorConfig Separator { get; set; } = new();
        public ButtonConfig OkButton { get; set; } = new();
        public FillStacksButtonConfig? FillStacksButton { get; set; } = new();
        public StoreAllButtonConfig? StoreAllButton { get; set; } = new();
        public LoadingTextConfig LoadingText { get; set; } = new();

        /// <summary>
        /// 从 JSON 文件加载菜单配置。
        /// </summary>
        public static MenuConfig Load(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<MenuConfig>(json) ?? new MenuConfig();
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance?.Monitor.Log($"Failed to load MenuConfig: {ex.Message}", StardewModdingAPI.LogLevel.Error);
            }

            // 如果加载失败，则返回默认配置
            return new MenuConfig();
        }
    }

    public class MenuDimensions
    {
        public int Width { get; set; } = 1100;
        public int Height { get; set; } = 920;
    }

    public class TitleConfig
    {
        public string Text { get; set; } = "奇点存储 (Singularity Storage)";
        public int OffsetY { get; set; } = 50;
    }

    public class HeaderConfig
    {
        public int OffsetY { get; set; } = 100;
        public int Height { get; set; } = 52;
        public int Padding { get; set; } = 32;
    }

    public class SearchBarConfig
    {
        public int OffsetX { get; set; } = 180;
        public int Width { get; set; } = 500;
        public int Height { get; set; } = 40;
        public string Placeholder { get; set; } = "Search...";
    }

    public class PageButtonsConfig
    {
        public int PrevOffsetX { get; set; } = 44;
        public int NextOffsetX { get; set; } = 108;
        public int Width { get; set; } = 44;
        public int Height { get; set; } = 40;
    }

    public class InventoryConfig
    {
        public int OffsetX { get; set; } = 60;
        public int OffsetY { get; set; } = 180;
        public int Columns { get; set; } = 14;
        public int Rows { get; set; } = 5;
        public int SlotSpacing { get; set; } = 4;
    }

    public class PlayerInventoryConfig
    {
        public int OffsetX { get; set; } = 60;
        public int OffsetFromBottom { get; set; } = 240;
    }

    public class SeparatorConfig
    {
        public int OffsetFromInventory { get; set; } = 40;
        public int Height { get; set; } = 16;
    }

    public class ButtonConfig
    {
        public int OffsetFromRight { get; set; } = 100;
        public int OffsetFromBottom { get; set; } = 100;
        public int Size { get; set; } = 64;
    }

    public class FillStacksButtonConfig : ButtonConfig
    {
         public TextureSourceConfig? TextureSource { get; set; } = new TextureSourceConfig();

         public FillStacksButtonConfig()
         {
             // Override base defaults for this specific button
             OffsetFromRight = 96;
             OffsetFromBottom = 816;
             Size = 48;
         }
    }

    public class TextureSourceConfig
    {
        public int X { get; set; } = 103;
        public int Y { get; set; } = 469;
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
    }

    public class StoreAllButtonConfig : ButtonConfig
    {
         public TextureSourceConfig? TextureSource { get; set; } = new TextureSourceConfig();

         public StoreAllButtonConfig()
         {
             // 位置在填充堆叠按钮左侧
             OffsetFromRight = 160;
             OffsetFromBottom = 816;
             Size = 48;
         }
    }

    public class LoadingTextConfig
    {
        public string Text { get; set; } = "加载中...";
        public int OffsetY { get; set; } = 120;
    }
}
