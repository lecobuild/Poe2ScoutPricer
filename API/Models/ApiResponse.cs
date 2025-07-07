// API/Models/ApiResponse.cs
using System.Text.Json.Serialization;

namespace Poe2ScoutPricer.API.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }

    public class ValidationError
    {
        [JsonPropertyName("detail")]
        public List<ValidationErrorDetail> Detail { get; set; } = new();
    }

    public class ValidationErrorDetail
    {
        [JsonPropertyName("loc")]
        public List<object> Location { get; set; } = new();

        [JsonPropertyName("msg")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class CurrencyItemByIdResponse
    {
        [JsonPropertyName("item")]
        public CurrencyItem Item { get; set; } = new();
    }
}