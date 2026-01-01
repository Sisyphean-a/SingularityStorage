using System;
using System.IO;
using Newtonsoft.Json;

namespace SingularityStorage.UI
{
    /// <summary>
    /// Configuration data for the Singularity Menu UI.
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
        public LoadingTextConfig LoadingText { get; set; } = new();

        /// <summary>
        /// Loads the menu configuration from the JSON file.
        /// </summary>
        public static MenuConfig Load(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<MenuConfig>(json) ?? new MenuConfig();
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance?.Monitor.Log($"Failed to load MenuConfig: {ex.Message}", StardewModdingAPI.LogLevel.Error);
            }

            // Return default config if loading fails
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
        public string Placeholder { get; set; } = "Search...";
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

    public class LoadingTextConfig
    {
        public string Text { get; set; } = "加载中...";
        public int OffsetY { get; set; } = 100;
    }
}
