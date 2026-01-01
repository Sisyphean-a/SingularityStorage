using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewUI.Framework;
using SingularityStorage.Network;

namespace SingularityStorage.UI
{
    public class SingularityMenu : IClickableMenu
    {
        private readonly SingularityMenuViewModel viewModel;
        private readonly IClickableMenu? innerMenu;

        public SingularityMenu(string sourceGuid)
        {
            this.viewModel = new SingularityMenuViewModel(sourceGuid);
            
            // Use ViewEngine to create the menu from asset
            var viewEngine = ModEntry.Instance?.ViewEngine;
            if (viewEngine != null)
            {
                var assetName = $"Mods/{ModEntry.Instance.ModManifest.UniqueID}/Views/SingularityMenu";
                this.innerMenu = viewEngine.CreateMenuFromAsset(assetName, this.viewModel);
            }
            else
            {
                ModEntry.Instance?.Monitor.Log("ViewEngine not available, cannot create menu.", LogLevel.Error);
            }
        }

        public override void draw(SpriteBatch b)
        {
            this.innerMenu?.draw(b);
        }

        public override void update(GameTime time)
        {
            this.innerMenu?.update(time);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            this.innerMenu?.receiveLeftClick(x, y, playSound);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            this.innerMenu?.receiveRightClick(x, y, playSound);
        }

        public override void performHoverAction(int x, int y)
        {
            this.innerMenu?.performHoverAction(x, y);
        }

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            this.viewModel.UpdateFromNetwork(packet);
        }
    }
}
