using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using HarmonyLib;

namespace SingularityStorage
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The singleton instance of this mod.</summary>
        public static ModEntry? Instance { get; private set; }

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            this.Monitor.Log("Singularity Storage initializing...", LogLevel.Info);

            // Initialize Harmony
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();

            // Register Events
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            
            // Initialize Managers
            StorageManager.Initialize(this.Monitor, this.Helper.Data);
            
            // Initialize Handlers
            new InteractionHandler(this.Helper, this.Monitor);
            
            // Initialize Network
            Network.NetworkManager.Initialize(this.Helper, this.Monitor);
        }

        /*********
        ** Private methods
        *********/
        
        /// <summary>Raised when an asset is requested.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // 1. Load the texture
            if (e.Name.IsEquivalentTo($"Mods/{this.ModManifest.UniqueID}/SingularityChest"))
            {
                e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/SingularityChest.png", AssetLoadPriority.Medium);
            }

            // 2. Add the BigCraftable data
            if (e.Name.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;
                    var itemData = new StardewValley.GameData.BigCraftables.BigCraftableData
                    {
                        Name = "SingularityChest", // Internal name must be stable/English
                        DisplayName = this.Helper.Translation.Get("chest.name"), // Localized name
                        Price = 1000,
                        Description = this.Helper.Translation.Get("chest.description"),
                        Texture = $"Mods/{this.ModManifest.UniqueID}/SingularityChest",
                        SpriteIndex = 0,
                        Fragility = 0,
                        CanBePlacedOutdoors = true,
                        IsLamp = false
                    };
                    data[$"{this.ModManifest.UniqueID}_SingularityChest"] = itemData;
                });
            }

            // 3. Add the crafting recipe
            if (e.Name.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, string>().Data;
                    // Format: materials / output quantity / output id / type / skill unlock
                    // Iridium Bar (337) x10, Gold Bar (336) x5, Battery Pack (787) x1
                    string itemId = $"{this.ModManifest.UniqueID}_SingularityChest";
                    // Using "true" for BigCraftable
                    data["Singularity Chest"] = $"337 1 336 5 787 1/Home/{itemId}/true/default"; 
                });
            }
        }

        /// <summary>Raised after the player loads a save slot.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.Monitor.Log("Save loaded. Initializing storage systems...", LogLevel.Debug);
            StorageManager.ClearCache();
            
            // Ensure player knows the recipe
            if (!Game1.player.craftingRecipes.ContainsKey("Singularity Chest"))
            {
                Game1.player.craftingRecipes.Add("Singularity Chest", 0);
            }
        }

        /// <summary>Raised before the game saves data.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Monitor.Log("Saving data...", LogLevel.Debug);
            StorageManager.SaveAll();
        }
    }
}
