using StardewValley;

namespace SingularityStorage.Network
{
    public class NetworkPacket
    {
        public string? SourceGuid { get; set; }
        public PacketType Type { get; set; }
        
        // 数据负荷 (根据包类型可能为空)
        // 目前我们使用简单的字段。
        
        // 用于 RequestPage (请求页面)
        public int PageIndex { get; set; }
        public string? SearchQuery { get; set; }
        
        // 用于 RespondPage (响应页面)
        public List<Item?>? Items { get; set; }
        public int TotalItems { get; set; }
        
        // 用于 Action (存入/取出)
        public string? ItemId { get; set; }
        public int Quantity { get; set; }
        public bool IsDeposit { get; set; }
    }

    public enum PacketType
    {
        RequestView, // 客户端请求初始数据/页面
        RespondView, // 主机发送页面数据
        RequestTransfer, // 客户端请求移动物品
        NotifyUpdate // 主机通知客户端内容已更改
    }
}
