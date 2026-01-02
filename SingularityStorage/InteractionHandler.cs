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

                    // Check for upgrade item
                    if (this.HandleUpgrade(obj, Game1.player.CurrentItem))
                    {
                        return;
                    }
                    
                    this.OpenStorage(obj);
                }
            }
        }

        private bool HandleUpgrade(StardewValley.Object chest, Item? item)
        {
            if (item == null) return false;
            
            // Define upgrade amounts
            int increment = 0;
            if (item.ItemId == "Singularity.Storage_T1_Comp") increment = 36;
            else if (item.ItemId == "Singularity.Storage_T2_Comp") increment = 100;
            else if (item.ItemId == "Singularity.Storage_T3_Comp") increment = 999;
            
            if (increment > 0)
            {
                // Ensure GUID exists
                if (!chest.modData.ContainsKey("SingularityData_GUID"))
                {
                    chest.modData["SingularityData_GUID"] = Guid.NewGuid().ToString();
                }

                string guid = chest.modData["SingularityData_GUID"];
                
                // Perform upgrade
                StorageManager.UpgradeCapacity(guid, increment);
                
                // Consume item
                Game1.player.reduceActiveItemByOne();
                
                // Feedback
                Game1.playSound("bubbles");
                Game1.addHUDMessage(new HUDMessage($"Storage Upgraded! +{increment} Capacity", 2));
                
                return true;
            }
            
            return false;
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
