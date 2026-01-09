using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace SingularityStorage.Framework.Content
{
    /// <summary>
    /// 处理游戏资源 (Assets) 的加载和编辑。
    /// </summary>
    public class ContentRegistry
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly string _modId;

        public ContentRegistry(IModHelper helper, IMonitor monitor, string modId)
        {
            _helper = helper;
            _monitor = monitor;
            _modId = modId;

            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // 1. 加载纹理 assets
            // 匹配 Mods/{ModId}/{AssetName}
            if (e.Name.StartsWith($"Mods/{_modId}/"))
            {
                var assetName = e.Name.Name.Split('/').Last(); // 获取最后一部分
                
                // 简单的映射关系，如果文件名和 Asset 名一致
                // 目前定义的 Asset Key: SingularityChest, Upgrade_T1, Upgrade_T2, Upgrade_T3
                // 对应的文件: assets/SingularityChest.png, assets/SilverProcessor.png, etc.

                string? filename = null;
                switch (assetName)
                {
                    case ItemDefinitions.TextureChest:
                        filename = "SingularityChest.png";
                        break;
                    case ItemDefinitions.TextureUpgradeT1:
                        filename = "SilverProcessor.png";
                        break;
                    case ItemDefinitions.TextureUpgradeT2:
                        filename = "GoldCircuit.png";
                        break;
                    case ItemDefinitions.TextureUpgradeT3:
                        filename = "VoidRune.png";
                        break;
                }

                if (filename != null)
                {
                    e.LoadFromModFile<Texture2D>($"assets/{filename}", AssetLoadPriority.Medium);
                }
            }

            // 2. 注入 BigCraftables (机器/家具)
            if (e.Name.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, BigCraftableData>().Data;
                    foreach (var kvp in ItemDefinitions.GetBigCraftables())
                    {
                        data[kvp.Key] = kvp.Value;
                    }
                });
            }

            // 3. 注入 Objects (物品)
            if (e.Name.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, ObjectData>().Data;
                    foreach (var kvp in ItemDefinitions.GetObjects())
                    {
                        data[kvp.Key] = kvp.Value;
                    }
                });
            }

            // 4. 注入 CraftingRecipes (配方)
            if (e.Name.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, string>().Data;
                    foreach (var recipe in ItemDefinitions.GetRecipes())
                    {
                        data[recipe.Name] = recipe.ToGameString();
                    }
                });
            }
        }
    }
}
