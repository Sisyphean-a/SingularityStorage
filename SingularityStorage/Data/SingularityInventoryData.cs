using System.Runtime.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;
using StardewValley;

namespace SingularityStorage.Data
{
    /// <summary>
    /// Represents the data model for a single Singularity Storage unit.
    /// This data is serialized to a separate JSON file, not the main save.
    /// </summary>
    public class SingularityInventoryData
    {
        /// <summary>The unique ID of this storage unit.</summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// The maximum number of items (not stacks) that can be stored.
        /// Defaults to 36.
        /// </summary>
        public int MaxCapacity { get; set; } = 70;

        /// <summary>
        /// The massive inventory storage.
        /// Key: Qualified Item ID (e.g., "(O)128")
        /// Value: List of Item stacks.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, List<Item>> Inventory { get; set; } = new Dictionary<string, List<Item>>();

        /// <summary>
        /// Comparison/Backup storage for serialization.
        /// Stores the XML representation of items to ensure 100% fidelity.
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
            // Use standard XmlSerializer for Item
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
                            // Fix stack size or other transient properties if needed
                            // item.fixStackSize(); 
                            itemList.Add(item);
                        }
                    }
                    catch
                    {
                        // Ignore corrupted items
                    }
                }
                this.Inventory[kvp.Key] = itemList;
            }
        }
    }
}
