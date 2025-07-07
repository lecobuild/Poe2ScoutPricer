// API/CollectiveApiData.cs
using Poe2ScoutPricer.API.Models;

namespace Poe2ScoutPricer.API
{
    public class CollectiveApiData
    {
        public CategoryResponse Categories { get; set; } = new();
        public Dictionary<string, CurrencyItemsResponse> CurrencyItems { get; set; } = new();
        public Dictionary<string, UniqueItemsResponse> UniqueItems { get; set; } = new();
        public UniqueBaseItemsResponse UniqueBaseItems { get; set; } = new();
        public List<League> Leagues { get; set; } = new();
        public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
        
        public double? DivinePrice => Leagues.FirstOrDefault(l => l.Value == "Standard")?.DivinePrice;

        public bool IsDataLoaded => Categories.UniqueCategories.Any() || Categories.CurrencyCategories.Any();

        public void Clear()
        {
            Categories = new CategoryResponse();
            CurrencyItems.Clear();
            UniqueItems.Clear();
            UniqueBaseItems = new UniqueBaseItemsResponse();
            Leagues.Clear();
            LastUpdateTime = DateTime.UtcNow;
        }
    }
}