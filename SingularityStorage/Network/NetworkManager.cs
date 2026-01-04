using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SingularityStorage.Network
{
    public static class NetworkManager
    {
        private static IModHelper? _helper;
        private static IMonitor? _monitor;
        
        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            _helper = helper;
            _monitor = monitor;
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

            _helper!.Multiplayer.SendMessage(packet, "RequestView", modIDs: new[] { _helper.ModRegistry.ModID });
        }

        private static void OnMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (_helper == null || e.FromModID != _helper.ModRegistry.ModID) return;

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
                // 如果 UI 已打开，则访问它
                if (Game1.activeClickableMenu is UI.SingularityMenu menu)
                {
                     menu.UpdateFromNetwork(packet); 
                }
            }
        }

        private static void HandleRequestView(long playerId, NetworkPacket packet)
        {
            if (packet.SourceGuid == null) return;

            // 主机逻辑：读取存储、过滤、切片并传回
            var data = StorageManager.GetInventory(packet.SourceGuid);
            var allItems = data.Inventory.Values.SelectMany(x => x).ToList();
            
            // 过滤
            if (!string.IsNullOrEmpty(packet.SearchQuery))
            {
                allItems = allItems.Where(i => i.DisplayName.Contains(packet.SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            var totalItems = allItems.Count;
            // 切片 (分页)
            var skip = packet.PageIndex * 36;
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
