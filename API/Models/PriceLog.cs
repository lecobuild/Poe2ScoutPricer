// API/Models/PriceLog.cs
using System.Text.Json.Serialization;

namespace Poe2ScoutPricer.API.Models
{
    public class PriceLog
    {
        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}