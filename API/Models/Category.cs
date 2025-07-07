// API/Models/Category.cs
using System.Text.Json.Serialization;

namespace Poe2ScoutPricer.API.Models
{
    public class Category
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("apiId")]
        public string ApiId { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }

    public class CategoryResponse
    {
        [JsonPropertyName("unique_categories")]
        public List<Category> UniqueCategories { get; set; } = new();

        [JsonPropertyName("currency_categories")]
        public List<Category> CurrencyCategories { get; set; } = new();
    }
}
