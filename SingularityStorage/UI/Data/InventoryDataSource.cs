using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using SingularityStorage.Data;

namespace SingularityStorage.UI.Data
{
    public class InventoryDataSource
    {
        private List<Item?> _fullItems = new List<Item?>();
        private List<Item?> _filteredItems = new List<Item?>();
        
        public int TotalCount => _filteredItems.Count;
        public IReadOnlyList<Item?> FullItems => _fullItems;

        public void UpdateSource(List<Item?> items)
        {
            this._fullItems = items ?? new List<Item?>();
            // 注意：更新源数据后通常需要重新应用过滤器，但由调用者控制是为了避免不必要的重复计算
            // 建议调用者在 UpdateSource 后显式调用 ApplyFilter
        }

        public void ApplyFilter(string searchText, string categoryGroup, int? subCategory)
        {
            var query = searchText?.Trim() ?? "";
            
            IEnumerable<Item?> items = this._fullItems;

            // 1. 按分类组过滤
            if (categoryGroup != "全部")
            {
                items = items.Where(item => CategoryData.IsItemInGroup(item, categoryGroup));
                
                // 2. 按子分类过滤
                if (subCategory.HasValue && subCategory.Value != -9999)
                {
                    items = items.Where(item => item != null && item.Category == subCategory.Value);
                }
            }

            // 3. 按搜索关键词过滤
            if (!string.IsNullOrEmpty(query))
            {
                items = items.Where(item => item != null && item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            this._filteredItems = items.ToList();
        }

        public List<Item> GetPage(int pageIndex, int pageSize)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 36;

            var startIndex = pageIndex * pageSize;
            return this._filteredItems
                .Skip(startIndex)
                .Take(pageSize)
                .OfType<Item>() // 过滤掉潜在的 null，虽然 FilteredItems 理论上不含 null（如果 Where 没过滤掉的话，在此加强安全性）
                .ToList();
        }
        
        public int GetTotalPages(int pageSize)
        {
             if (pageSize <= 0) return 1;
             var totalPages = (int)Math.Ceiling(this.TotalCount / (double)pageSize);
             return totalPages == 0 ? 1 : totalPages;
        }
    }
}
