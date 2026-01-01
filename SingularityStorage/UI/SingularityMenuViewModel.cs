using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewUI.Graphics; // Required for Sprite
using SingularityStorage.Network;

namespace SingularityStorage.UI
{
    public class InventoryItemViewModel
    {
        public string DisplayName { get; }
        public Sprite Sprite { get; }

        public InventoryItemViewModel(Item item)
        {
            this.DisplayName = item.DisplayName;
            
            try 
            {
                var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
                this.Sprite = new Sprite(data.GetTexture(), data.GetSourceRect());
            }
            catch
            {
                this.Sprite = new Sprite(Game1.mouseCursors, new Rectangle(0, 0, 16, 16));
            }
        }
    }

    public class SingularityMenuViewModel : INotifyPropertyChanged
    {
        private readonly string SourceGuid;
        private List<Item> FullInventory = new();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                    UpdateFilter();
            }
        }

        private IEnumerable<InventoryItemViewModel> _filteredInventory = new List<InventoryItemViewModel>();
        public IEnumerable<InventoryItemViewModel> FilteredInventory
        {
            get => _filteredInventory;
            private set => SetField(ref _filteredInventory, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetField(ref _isLoading, value))
                    OnPropertyChanged(nameof(IsLoadingVisibility));
            }
        }

        // StardewUI uses "visible", "hidden", or "collapsed" for visibility
        public string IsLoadingVisibility => IsLoading ? "visible" : "hidden";

        // Display item count
        public string ItemCountText => $"物品数量: {FilteredInventory.Count()} / {FullInventory.Count}";

        public SingularityMenuViewModel(string sourceGuid)
        {
            this.SourceGuid = sourceGuid;
            LoadData();
        }

        private void LoadData()
        {
            if (Context.IsMainPlayer)
            {
                var data = StorageManager.GetInventory(this.SourceGuid);
                this.FullInventory = data.Inventory.Values.SelectMany(x => x).ToList();
                ModEntry.Instance?.Monitor.Log($"Loaded {FullInventory.Count} items from storage", LogLevel.Debug);
                UpdateFilter();
            }
            else
            {
                this.IsLoading = true;
                NetworkManager.SendRequestView(this.SourceGuid, 0, "");
            }
        }

        public void UpdateFromNetwork(NetworkPacket packet)
        {
            if (packet.SourceGuid != this.SourceGuid) return;

            var pageItems = packet.Items ?? new List<Item?>();
            this.FullInventory = pageItems.Where(i => i != null).Cast<Item>().ToList();
            UpdateFilter();
            this.IsLoading = false;
        }

        private void UpdateFilter()
        {
            IEnumerable<Item> result;
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                result = FullInventory;
            }
            else
            {
                result = FullInventory
                    .Where(item => item.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            FilteredInventory = result.Select(i => new InventoryItemViewModel(i)).ToList();
            ModEntry.Instance?.Monitor.Log($"FilteredInventory count: {FilteredInventory.Count()}, SearchText: '{SearchText}'", LogLevel.Debug);
            
            // Notify UI that item count changed
            OnPropertyChanged(nameof(ItemCountText));
        }

        public void OnItemClicked(InventoryItemViewModel item)
        {
            Game1.playSound("dwop");
            // TODO: Implement item handling (take/give)
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
