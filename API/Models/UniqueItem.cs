// API/Models/UniqueItem.cs
using System.Text.Json.Serialization;

namespace Poe2ScoutPricer.API.Models
{
    public class UniqueItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("categoryApiId")]
        public string CategoryApiId { get; set; } = string.Empty;

        [JsonPropertyName("itemMetadata")]
        public Dictionary<string, object>? ItemMetadata { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("isChanceable")]
        public bool IsChanceable { get; set; }

        [JsonPropertyName("priceLogs")]
        public List<PriceLog?> PriceLogs { get; set; } = new();

        [JsonPropertyName("currentPrice")]
        public double? CurrentPrice { get; set; }
    }

    public class UniqueItemsResponse
    {
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<UniqueItem> Items { get; set; } = new();
    }

    public class UniqueBaseItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("itemMetadata")]
        public Dictionary<string, object>? ItemMetadata { get; set; }

        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("apiId")]
        public string ApiId { get; set; } = string.Empty;

        [JsonPropertyName("priceLogs")]
        public List<PriceLog?> PriceLogs { get; set; } = new();

        [JsonPropertyName("currentPrice")]
        public double? CurrentPrice { get; set; }

        [JsonPropertyName("averageUniquePrice")]
        public double? AverageUniquePrice { get; set; }

        [JsonPropertyName("isChanceable")]
        public bool? IsChanceable { get; set; }
    }

    public class UniqueBaseItemsResponse
    {
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<UniqueBaseItem> Items { get; set; } = new();
    }

    public class UniquesByBaseNameResponse
    {
        [JsonPropertyName("items")]
        public List<UniqueItem> Items { get; set; } = new();
    }
}