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
        public FillStacksButtonConfig? FillStacksButton { get; set; }
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
        public int Width { get; set; } = 950;
        public int Height { get; set; } = 750;
    }

    public class TitleConfig
    {
        public string Text { get; set; } = "奇点存储 (Singularity Storage)";
        public int OffsetY { get; set; } = 40;
    }

    public class HeaderConfig
    {
        public int OffsetY { get; set; } = 80;
        public int Height { get; set; } = 64;
        public int Padding { get; set; } = 16;
    }

    public class SearchBarConfig
    {
        public int OffsetX { get; set; } = 200;
        public int Width { get; set; } = 400;
        public int Height { get; set; } = 36;
        public string Placeholder { get; set; } = "搜索...";
    }

    public class PageButtonsConfig
    {
        public int PrevOffsetX { get; set; } = 42;
        public int NextOffsetX { get; set; } = 106;
        public int Width { get; set; } = 48;
        public int Height { get; set; } = 44;
    }

    public class InventoryConfig
    {
        public int OffsetX { get; set; } = 32;
        public int OffsetY { get; set; } = 180;
        public int Columns { get; set; } = 9;
        public int Rows { get; set; } = 3;
        public int SlotSpacing { get; set; } = 0;
    }

    public class PlayerInventoryConfig
    {
        public int OffsetX { get; set; } = 32;
        public int OffsetFromBottom { get; set; } = 220;
    }

    public class SeparatorConfig
    {
        public int OffsetFromInventory { get; set; } = 32;
        public int Height { get; set; } = 16;
    }

    public class ButtonConfig
    {
        public int OffsetFromRight { get; set; } = 80;
        public int OffsetFromBottom { get; set; } = 80;
        public int Size { get; set; } = 64;
    }

    public class FillStacksButtonConfig : ButtonConfig
    {
         public TextureSourceConfig? TextureSource { get; set; }
    }

    public class TextureSourceConfig
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class LoadingTextConfig
    {
        public string Text { get; set; } = "加载中...";
        public int OffsetY { get; set; } = 100;
    }
}
