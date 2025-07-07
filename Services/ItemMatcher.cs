
// Services/ItemMatcher.cs
using Poe2ScoutPricer.API.Models;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.Services
{
    public interface IItemMatcher
    {
        CurrencyItem? FindCurrencyItem(string itemName, string categoryApiId, IEnumerable<CurrencyItem> items);
        UniqueItem? FindUniqueItem(string itemName, string baseName, string categoryApiId, IEnumerable<UniqueItem> items);
        double CalculateSimilarity(string input, string target);
        string NormalizeItemName(string itemName);
    }

    public class ItemMatcher : IItemMatcher
    {
        private static readonly Dictionary<string, string> ShardMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Transmutation Shard", "Orb of Transmutation" },
            { "Alteration Shard", "Orb of Alteration" },
            { "Annulment Shard", "Orb of Annulment" },
            { "Exalted Shard", "Exalted Orb" },
            { "Mirror Shard", "Mirror of Kalandra" },
            { "Regal Shard", "Regal Orb" },
            { "Alchemy Shard", "Orb of Alchemy" },
            { "Chaos Shard", "Chaos Orb" },
            { "Ancient Shard", "Ancient Orb" },
            { "Engineer's Shard", "Engineer's Orb" },
            { "Harbinger's Shard", "Harbinger's Orb" },
            { "Horizon Shard", "Orb of Horizons" },
            { "Binding Shard", "Orb of Binding" },
            { "Scroll Fragment", "Scroll of Wisdom" },
            { "Chance Shard", "Orb of Chance" }
        };

        public CurrencyItem? FindCurrencyItem(string itemName, string categoryApiId, IEnumerable<CurrencyItem> items)
        {
            if (string.IsNullOrEmpty(itemName))
                return null;

            var normalizedName = NormalizeItemName(itemName);
            var itemsList = items.ToList();

            // Check if it's a shard and try to find the full item
            if (ShardMapping.TryGetValue(itemName, out var fullItemName))
            {
                var fullItem = FindBestMatch(fullItemName, itemsList, item => item.Text);
                if (fullItem != null)
                {
                    Logger.LogDebug($"Found shard mapping: {itemName} -> {fullItemName}");
                    return fullItem;
                }
            }

            // Exact match by text
            var exactMatch = itemsList.FirstOrDefault(item => 
                item.Text.EqualsIgnoreCase(itemName));
            if (exactMatch != null)
                return exactMatch;

            // Exact match by apiId
            exactMatch = itemsList.FirstOrDefault(item => 
                item.ApiId.EqualsIgnoreCase(normalizedName));
            if (exactMatch != null)
                return exactMatch;

            // Fuzzy match
            return FindBestMatch(normalizedName, itemsList, item => item.Text);
        }

        public UniqueItem? FindUniqueItem(string itemName, string baseName, string categoryApiId, IEnumerable<UniqueItem> items)
        {
            if (string.IsNullOrEmpty(itemName))
                return null;

            var normalizedName = NormalizeItemName(itemName);
            var itemsList = items.ToList();

            // Exact match by name
            var exactMatch = itemsList.FirstOrDefault(item => 
                item.Name.EqualsIgnoreCase(itemName));
            if (exactMatch != null)
                return exactMatch;

            // Exact match by text
            exactMatch = itemsList.FirstOrDefault(item => 
                item.Text.EqualsIgnoreCase(itemName));
            if (exactMatch != null)
                return exactMatch;

            // Try to match by base name if provided
            if (!string.IsNullOrEmpty(baseName))
            {
                var baseMatch = itemsList.FirstOrDefault(item => 
                    item.Type.EqualsIgnoreCase(baseName));
                if (baseMatch != null)
                    return baseMatch;
            }

            // Fuzzy match by name
            var fuzzyMatch = FindBestMatch(normalizedName, itemsList, item => item.Name);
            if (fuzzyMatch != null)
                return fuzzyMatch;

            // Fuzzy match by text
            return FindBestMatch(normalizedName, itemsList, item => item.Text);
        }

        public double CalculateSimilarity(string input, string target)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target))
                return 0;

            var normalizedInput = NormalizeItemName(input);
            var normalizedTarget = NormalizeItemName(target);

            if (normalizedInput == normalizedTarget)
                return 1.0;

            if (normalizedTarget.Contains(normalizedInput) || normalizedInput.Contains(normalizedTarget))
                return 0.8;

            // Levenshtein distance
            var distance = CalculateLevenshteinDistance(normalizedInput, normalizedTarget);
            var maxLength = Math.Max(normalizedInput.Length, normalizedTarget.Length);
            
            return maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;
        }

        public string NormalizeItemName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return string.Empty;

            return itemName
                .Replace("'", "'")  // Replace curly apostrophe with straight
                .Replace(""", "\"")  // Replace smart quotes
                .Replace(""", "\"")
                .Trim()
                .ToLowerInvariant();
        }

        private T? FindBestMatch<T>(string searchTerm, IEnumerable<T> items, Func<T, string> nameSelector)
        {
            var bestMatch = default(T);
            var bestSimilarity = 0.0;
            const double minimumSimilarity = 0.6;

            foreach (var item in items)
            {
                var itemName = nameSelector(item);
                var similarity = CalculateSimilarity(searchTerm, itemName);

                if (similarity > bestSimilarity && similarity >= minimumSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = item;
                }
            }

            if (bestMatch != null)
            {
                Logger.LogDebug($"Best match for '{searchTerm}': '{nameSelector(bestMatch)}' (similarity: {bestSimilarity:0.00})");
            }

            return bestMatch;
        }

        private static int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return source.Length;

            var sourceLength = source.Length;
            var targetLength = target.Length;
            var matrix = new int[sourceLength + 1, targetLength + 1];

            for (var i = 0; i <= sourceLength; i++)
                matrix[i, 0] = i;

            for (var j = 0; j <= targetLength; j++)
                matrix[0, j] = j;

            for (var i = 1; i <= sourceLength; i++)
            {
                for (var j = 1; j <= targetLength; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[sourceLength, targetLength];
        }
    }
}