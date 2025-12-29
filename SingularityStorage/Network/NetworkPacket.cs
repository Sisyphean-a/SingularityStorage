using System.Collections.Generic;
using StardewValley;

namespace SingularityStorage.Network
{
    public class NetworkPacket
    {
        public string? SourceGuid { get; set; }
        public PacketType Type { get; set; }
        
        // Data Payloads (nullable depending on type)
        // We use simple fields for now.
        
        // For RequestPage
        public int PageIndex { get; set; }
        public string? SearchQuery { get; set; }
        
        // For RespondPage
        public List<Item?>? Items { get; set; }
        public int TotalItems { get; set; }
        
        // For Action (Deposit/Withdraw)
        public string? ItemId { get; set; }
        public int Quantity { get; set; }
        public bool IsDeposit { get; set; }
    }

    public enum PacketType
    {
        RequestView, // Client requests initial data/page
        RespondView, // Host sends page data
        RequestTransfer, // Client wants to move item
        NotifyUpdate // Host tells clients content changed
    }
}
