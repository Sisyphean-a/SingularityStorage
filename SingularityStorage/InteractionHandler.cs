using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SingularityStorage.UI;

namespace SingularityStorage
{
    public class InteractionHandler
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        
        // Define the Qualified Item ID for our custom object.
        // Format: (BC)ModID_ItemId
        private const string SingularityChestId = "(BC)Singularity.Storage_SingularityChest";

        public InteractionHandler(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Only handle right-clicks (Check actions)
            if (!e.Button.IsActionButton()) return;

            Vector2 clickedTile = e.Cursor.Tile;
            // Check if there is an object at the clicked tile
            if (Game1.currentLocation.Objects.TryGetValue(clickedTile, out StardewValley.Object obj))
            {
                // In 1.6, we check QualifiedItemId
                // Since CP uses the ModId as prefix, we need to match what we put in content.json
                // manifest.json ID is "Singularity.Storage", content.json uses {{ModId}}_SingularityChest
                if (obj.QualifiedItemId == SingularityChestId || obj.ItemId == "Singularity.Storage_SingularityChest")
                {
                    // Suppress default action (which might be just playing a sound or shaking)
                    this.Helper.Input.Suppress(e.Button);
                    
                    this.OpenStorage(obj);
                }
            }
        }

        private void OpenStorage(StardewValley.Object chestObj)
        {
            // Ensure the chest has a GUID
            if (!chestObj.modData.ContainsKey("SingularityData_GUID"))
            {
                chestObj.modData["SingularityData_GUID"] = Guid.NewGuid().ToString();
            }

            string guid = chestObj.modData["SingularityData_GUID"];
            
            this.Monitor.Log($"Opening Singularity Storage: {guid}", LogLevel.Debug);
            
            // Open the UI
            Game1.activeClickableMenu = new SingularityMenu(guid);
        }
    }
}
