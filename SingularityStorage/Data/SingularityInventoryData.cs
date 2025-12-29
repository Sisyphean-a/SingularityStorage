using System.Collections.Generic;
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
        public string GUID { get; set; } = string.Empty;

        /// <summary>
        /// The massive inventory storage.
        /// Key: Qualified Item ID (e.g., "(O)128")
        /// Value: List of Item stacks.
        /// </summary>
        public Dictionary<string, List<Item>> Inventory { get; set; } = new Dictionary<string, List<Item>>();

        public SingularityInventoryData() { }

        public SingularityInventoryData(string guid)
        {
            this.GUID = guid;
        }
    }
}
