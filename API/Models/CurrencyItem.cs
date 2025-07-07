// API/Models/CurrencyItem.cs
using System.Text.Json.Serialization;

namespace Poe2ScoutPricer.API.Models
{
    public class CurrencyItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }

        [JsonPropertyName("currencyCategoryId")]
        public int CurrencyCategoryId { get; set; }

        [JsonPropertyName("apiId")]
        public string ApiId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("categoryApiId")]
        public string CategoryApiId { get; set; } = string.Empty;

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("itemMetadata")]
        public Dictionary<string, object>? ItemMetadata { get; set; }

        [JsonPropertyName("priceLogs")]
        public List<PriceLog?> PriceLogs { get; set; } = new();

        [JsonPropertyName("currentPrice")]
        public double? CurrentPrice { get; set; }
    }

    public class CurrencyItemsResponse
    {
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<CurrencyItem> Items { get; set; } = new();
    }
}