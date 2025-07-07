// Utils/Extensions.cs
using System.Globalization;

namespace Poe2ScoutPricer.Utils
{
    public static class Extensions
    {
        public static string FormatPrice(this double? price, int decimals = 2)
        {
            if (price == null || price == 0)
                return "0c";

            if (price < 0.01)
                return "~0c";

            return $"{Math.Round(price.Value, decimals):0.##}c";
        }

        public static string FormatDivinePrice(this double? price, double? divinePrice)
        {
            if (price == null || price == 0 || divinePrice == null || divinePrice == 0)
                return "0c";

            var priceValue = price.Value;
            var divineValue = divinePrice.Value;

            if (priceValue >= divineValue)
            {
                var divines = priceValue / divineValue;
                if (divines >= 1)
                {
                    return $"{Math.Round(divines, 2):0.##}d";
                }
            }

            return priceValue.FormatPrice();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
        {
            return collection == null || !collection.Any();
        }

        public static T? GetValueOrDefault<TKey, T>(this Dictionary<TKey, T> dictionary, TKey key) where TKey : notnull
        {
            return dictionary.TryGetValue(key, out var value) ? value : default;
        }

        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnoreCase(this string source, string toCheck)
        {
            return source.Equals(toCheck, StringComparison.OrdinalIgnoreCase);
        }
    }
}