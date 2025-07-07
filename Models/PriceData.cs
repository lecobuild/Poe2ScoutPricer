// Models/PriceData.cs
using Poe2ScoutPricer.Models;

namespace Poe2ScoutPricer.Models
{
    public class PriceData
    {
        public double MinChaosValue { get; set; }
        public double MaxChaosValue { get; set; }
        public double CurrentPrice { get; set; }
        public double ChangeInLast7Days { get; set; }
        public ItemTypes ItemType { get; set; }
        public string DetailsId { get; set; } = string.Empty;
        public string CategoryApiId { get; set; } = string.Empty;
        public bool IsChanceable { get; set; }
        public List<double> PriceHistory { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public bool HasValidPrice => CurrentPrice > 0 || MinChaosValue > 0 || MaxChaosValue > 0;

        public double GetBestPrice()
        {
            if (CurrentPrice > 0) return CurrentPrice;
            if (MinChaosValue > 0) return MinChaosValue;
            if (MaxChaosValue > 0) return MaxChaosValue;
            return 0;
        }

        public string GetPriceRange()
        {
            if (MinChaosValue > 0 && MaxChaosValue > 0 && Math.Abs(MinChaosValue - MaxChaosValue) > 0.01)
            {
                return $"{MinChaosValue:0.##} - {MaxChaosValue:0.##}c";
            }
            return GetBestPrice().ToString("0.##") + "c";
        }

        public override string ToString()
        {
            return $"Price: {GetBestPrice():0.##}c, Type: {ItemType}, Category: {CategoryApiId}";
        }
    }
}