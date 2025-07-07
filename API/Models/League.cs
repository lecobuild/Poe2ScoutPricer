// API/Models/League.cs
using System.Text.Json.Serialization;

namespace Poe2ScoutPricer.API.Models
{
    public class League
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("divinePrice")]
        public double DivinePrice { get; set; }
    }
}