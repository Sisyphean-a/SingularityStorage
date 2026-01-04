using System.Runtime.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;
using StardewValley;

namespace SingularityStorage.Data
{
    /// <summary>
    /// 代表单个奇点存储单元的数据模型。
    /// 此数据被序列化为单独的 JSON 文件，而不是存放在主存档中。
    /// </summary>
    public class SingularityInventoryData
    {
        /// <summary>此存储单元的唯一 ID。</summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// 可存储的最大物品数量（不是堆叠数）。
        /// 默认为 36。
        /// </summary>
        public int MaxCapacity { get; set; } = 70;

        /// <summary>
        /// 海量库存存储。
        /// 键：限定物品 ID（例如："(O)128"）
        /// 值：物品堆叠列表。
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, List<Item>> Inventory { get; set; } = new Dictionary<string, List<Item>>();

        /// <summary>
        /// 用于序列化的对比/备份存储。
        /// 存储物品的 XML 表示形式，以确保 100% 的保真度。
        /// </summary>
        [JsonProperty("Items")]
        public Dictionary<string, List<string>> SerializedInventory { get; set; } = new Dictionary<string, List<string>>();

        public SingularityInventoryData() { }

        public SingularityInventoryData(string guid)
        {
            this.Guid = guid;
        }

        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            this.SerializedInventory = new Dictionary<string, List<string>>();
            // 对 Item 使用标准的 XmlSerializer
            var serializer = new XmlSerializer(typeof(Item));

            foreach (var kvp in this.Inventory)
            {
                var xmlList = new List<string>();
                foreach (var item in kvp.Value)
                {
                    using var writer = new StringWriter();
                    serializer.Serialize(writer, item);
                    xmlList.Add(writer.ToString());
                }
                this.SerializedInventory[kvp.Key] = xmlList;
            }
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            this.Inventory = new Dictionary<string, List<Item>>();
            var serializer = new XmlSerializer(typeof(Item));

            foreach (var kvp in this.SerializedInventory)
            {
                var itemList = new List<Item>();
                foreach (var xml in kvp.Value)
                {
                    try
                    {
                        using var reader = new StringReader(xml);
                        var item = (Item?)serializer.Deserialize(reader);
                        if (item != null)
                        {
                            // 如果需要，修复堆叠大小或其他瞬态属性
                            // item.fixStackSize(); 
                            itemList.Add(item);
                        }
                    }
                    catch
                    {
                        // 忽略损坏的物品
                    }
                }
                this.Inventory[kvp.Key] = itemList;
            }
        }
    }
}
