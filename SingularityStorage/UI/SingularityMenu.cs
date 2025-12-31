using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewUI;
using StardewUI.Widgets;
using StardewUI.Layout;
using SingularityStorage.Network;

namespace SingularityStorage.UI
{
    public class SingularityMenu : ViewMenu<SingularityMenuViewModel>
    {
        public SingularityMenu(string sourceGuid) 
            : base(new SingularityMenuViewModel(sourceGuid))
        {
        }

        protected override IView CreateView(SingularityMenuViewModel viewModel)
        {
            // Load StarML from assets
            var assetName = "assets/views/SingularityMenu.sml";
            var sml = ModEntry.Instance.Helper.ModContent.Load<string>(assetName);
            return View.FromString(sml);
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            this.Context.UpdateFromNetwork(packet);
        }
    }
}
