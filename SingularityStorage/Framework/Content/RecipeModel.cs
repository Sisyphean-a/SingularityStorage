using System.Text;

namespace SingularityStorage.Framework.Content
{
    /// <summary>
    /// 表示一个制作配方的成分。
    /// </summary>
    public struct IngredientModel
    {
        /// <summary>物品的 ID (例如 "337" 或 "Singularity.Storage_T1_Comp")。</summary>
        public string ItemId { get; set; }

        /// <summary>所需数量。</summary>
        public int Count { get; set; }

        public IngredientModel(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public override string ToString()
        {
            return $"{ItemId} {Count}";
        }
    }

    /// <summary>
    /// 表示一个制作配方的数据模型。
    /// </summary>
    public class RecipeModel
    {
        /// <summary>配方名称 (也是配方的 ID)。</summary>
        public string Name { get; set; }

        /// <summary>所需的材料列表。</summary>
        public List<IngredientModel> Ingredients { get; set; } = new();

        /// <summary>输出物品的数量。</summary>
        public int OutputCount { get; set; } = 1;

        /// <summary>输出物品的 ID。</summary>
        public string OutputItemId { get; set; }

        /// <summary>这是否是一个 BigCraftable (机器/家具) 配方？如果是 false，则假定为 Object 配方。</summary>
        public bool IsBigCraftable { get; set; }

        /// <summary>解锁条件 (默认为 "default"，即初始解锁或随技能解锁，此处简化处理)。</summary>
        public string UnlockCondition { get; set; } = "default";
        
        /// <summary>技能解锁要求，例如 "Farming 1"。如果为 "default" 且此字段为空，则立即解锁。</summary>
        public string SkillUnlock { get; set; } = "default";

        public RecipeModel(string name, string outputItemId, int outputCount, bool isBigCraftable = false)
        {
            Name = name;
            OutputItemId = outputItemId;
            OutputCount = outputCount;
            IsBigCraftable = isBigCraftable;
        }

        public RecipeModel AddIngredient(string itemId, int count)
        {
            Ingredients.Add(new IngredientModel(itemId, count));
            return this;
        }

        /// <summary>
        /// 将配方转换为 SDV 要求的字符串格式。
        /// 格式：材料 / 输出数量 / 输出 ID / 类型 / 技能解锁
        /// </summary>
        public string ToGameString()
        {
            var sb = new StringBuilder();

            // 1. Ingredients: id count id count ...
            sb.Append(string.Join(" ", Ingredients));
            sb.Append('/');

            // 2. Output Field
            
            if (IsBigCraftable)
            {
                // 对于 BigCraftables，第二个字段通常是 "Home" (未使用的遗留字段)
                sb.Append("Home"); 
            }
            else
            {
                // 对于普通物品，第二个字段是产生数量
                sb.Append(OutputCount);
            }
            sb.Append('/');

            // 3. Output Item ID
            sb.Append(OutputItemId);
            sb.Append('/');

            // 4. Is BigCraftable?
            sb.Append(IsBigCraftable ? "true" : "false");
            sb.Append('/');

            // 5. Unlock Condition
            sb.Append(SkillUnlock);

            // 1.6+ 可能支持更多字段，目前保持基础兼容
            
            return sb.ToString();
        }
    }
}
