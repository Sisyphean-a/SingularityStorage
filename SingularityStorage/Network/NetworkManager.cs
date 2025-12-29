using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SingularityStorage.Network
{
    public static class NetworkManager
    {
        private static IModHelper? Helper;
        private static IMonitor? Monitor;
        
        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;
            helper.Events.Multiplayer.ModMessageReceived += OnMessageReceived;
        }

        public static void SendRequestView(string guid, int page, string query)
        {
            if (Context.IsMainPlayer) return;

            var packet = new NetworkPacket
            {
                Type = PacketType.RequestView,
                SourceGuid = guid,
                PageIndex = page,
                SearchQuery = query
            };

            Helper!.Multiplayer.SendMessage(packet, "RequestView", modIDs: new[] { Helper.ModRegistry.ModID });
        }

        private static void OnMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (Helper == null || e.FromModID != Helper.ModRegistry.ModID) return;

            if (e.Type == "RequestView" && Context.IsMainPlayer)
            {
                var packet = e.ReadAs<NetworkPacket>();
                if (packet.SourceGuid != null)
                {
                    HandleRequestView(e.FromPlayerID, packet);
                }
            }
            else if (e.Type == "RespondView" && !Context.IsMainPlayer)
            {
                var packet = e.ReadAs<NetworkPacket>();
                // Access UI if open
                if (Game1.activeClickableMenu is UI.SingularityMenu menu)
                {
                     menu.UpdateFromNetwork(packet); 
                }
            }
        }

        private static void HandleRequestView(long playerId, NetworkPacket packet)
        {
            if (packet.SourceGuid == null) return;

            // Host logic: read storage, filter, slice, send back
            var data = StorageManager.GetInventory(packet.SourceGuid);
            var allItems = data.Inventory.Values.SelectMany(x => x).ToList();
            
            // Filter
            if (!string.IsNullOrEmpty(packet.SearchQuery))
            {
                allItems = allItems.Where(i => i.DisplayName.Contains(packet.SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            int totalItems = allItems.Count;
            // Slice
            int skip = packet.PageIndex * 36;
            var pageItems = allItems.Skip(skip).Take(36).Cast<Item?>().ToList();
            
            var response = new NetworkPacket
            {
                Type = PacketType.RespondView,
                SourceGuid = packet.SourceGuid,
                Items = pageItems,
                TotalItems = totalItems
            };

            // Helper!.Multiplayer.SendMessage(response, "RespondView", playerIDs: new[] { playerId }, modIDs: new[] { Helper.ModRegistry.ModID });
        }
    }
}
