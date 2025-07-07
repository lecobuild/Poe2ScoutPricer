// Services/PriceService.cs
using Poe2ScoutPricer.API;
using Poe2ScoutPricer.API.Models;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.Services
{
    public interface IPriceService
    {
        Task<PriceData> GetItemPriceAsync(CustomItem item, string league);
        Task<bool> LoadAllDataAsync(string league);
        Task<bool> RefreshDataAsync(string league);
        bool IsDataLoaded { get; }
        DateTime LastUpdateTime { get; }
        double? DivinePrice { get; }
    }

    public class PriceService : IPriceService
    {
        private readonly IPoe2ScoutApi _apiClient;
        private readonly ICacheService _cacheService;
        private readonly IItemMatcher _itemMatcher;
        private readonly CollectiveApiData _collectiveData;

        public bool IsDataLoaded => _collectiveData.IsDataLoaded;
        public DateTime LastUpdateTime => _collectiveData.LastUpdateTime;
        public double? DivinePrice => _collectiveData.DivinePrice;

        public PriceService(IPoe2ScoutApi apiClient, ICacheService cacheService, IItemMatcher itemMatcher)
        {
            _apiClient = apiClient;
            _cacheService = cacheService;
            _itemMatcher = itemMatcher;
            _collectiveData = new CollectiveApiData();
        }

        public async Task<PriceData> GetItemPriceAsync(CustomItem item, string league)
        {
            var priceData = new PriceData
            {
                ItemType = ItemTypeExtensions.FromCategoryApiId(item.CategoryApiId),
                CategoryApiId = item.CategoryApiId
            };

            try
            {
                if (priceData.ItemType.IsCurrency())
                {
                    await GetCurrencyPriceAsync(item, league, priceData);
                }
                else if (priceData.ItemType.IsUnique())
                {
                    await GetUniquePriceAsync(item, league, priceData);
                }
                else
                {
                    Logger.LogDebug($"Unknown item type for item: {item.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting price for item '{item.Name}': {ex.Message}");
            }

            return priceData;
        }

        private async Task GetCurrencyPriceAsync(CustomItem item, string league, PriceData priceData)
        {
            if (!_collectiveData.CurrencyItems.TryGetValue(item.CategoryApiId, out var currencyResponse))
            {
                // Try to load specific category data
                var response = await _apiClient.GetCurrencyItemsAsync(item.CategoryApiId, league: league);
                if (response.IsSuccess && response.Data != null)
                {
                    currencyResponse = response.Data;
                    _collectiveData.CurrencyItems[item.CategoryApiId] = currencyResponse;
                }
                else
                {
                    Logger.LogError($"Failed to load currency data for category '{item.CategoryApiId}': {response.ErrorMessage}");
                    return;
                }
            }

            var matchedItem = _itemMatcher.FindCurrencyItem(item.Name, item.CategoryApiId, currencyResponse.Items);
            if (matchedItem != null)
            {
                priceData.CurrentPrice = matchedItem.CurrentPrice ?? 0;
                priceData.MinChaosValue = priceData.CurrentPrice;
                priceData.MaxChaosValue = priceData.CurrentPrice;
                priceData.DetailsId = matchedItem.ApiId;
                
                if (matchedItem.PriceLogs?.Any() == true)
                {
                    priceData.PriceHistory = matchedItem.PriceLogs
                        .Where(log => log != null)
                        .Select(log => log!.Price)
                        .ToList();
                }

                Logger.LogDebug($"Found currency price for '{item.Name}': {priceData.CurrentPrice}c");
            }
            else
            {
                Logger.LogDebug($"No currency match found for '{item.Name}' in category '{item.CategoryApiId}'");
            }
        }

        private async Task GetUniquePriceAsync(CustomItem item, string league, PriceData priceData)
        {
            if (!_collectiveData.UniqueItems.TryGetValue(item.CategoryApiId, out var uniqueResponse))
            {
                // Try to load specific category data
                var response = await _apiClient.GetUniqueItemsAsync(item.CategoryApiId, league: league);
                if (response.IsSuccess && response.Data != null)
                {
                    uniqueResponse = response.Data;
                    _collectiveData.UniqueItems[item.CategoryApiId] = uniqueResponse;
                }
                else
                {
                    Logger.LogError($"Failed to load unique data for category '{item.CategoryApiId}': {response.ErrorMessage}");
                    return;
                }
            }

            var matchedItem = _itemMatcher.FindUniqueItem(item.Name, item.BaseName, item.CategoryApiId, uniqueResponse.Items);
            if (matchedItem != null)
            {
                priceData.CurrentPrice = matchedItem.CurrentPrice ?? 0;
                priceData.MinChaosValue = priceData.CurrentPrice;
                priceData.MaxChaosValue = priceData.CurrentPrice;
                priceData.DetailsId = matchedItem.Name;
                priceData.IsChanceable = matchedItem.IsChanceable;
                
                if (matchedItem.PriceLogs?.Any() == true)
                {
                    priceData.PriceHistory = matchedItem.PriceLogs
                        .Where(log => log != null)
                        .Select(log => log!.Price)
                        .ToList();
                }

                Logger.LogDebug($"Found unique price for '{item.Name}': {priceData.CurrentPrice}c");
            }
            else
            {
                Logger.LogDebug($"No unique match found for '{item.Name}' in category '{item.CategoryApiId}'");
            }
        }

        public async Task<bool> LoadAllDataAsync(string league)
        {
            try
            {
                Logger.LogInfo($"Loading all data for league: {league}");

                // Load categories first
                var categoriesResponse = await _apiClient.GetCategoriesAsync();
                if (!categoriesResponse.IsSuccess || categoriesResponse.Data == null)
                {
                    Logger.LogError($"Failed to load categories: {categoriesResponse.ErrorMessage}");
                    return false;
                }

                _collectiveData.Categories = categoriesResponse.Data;

                // Load leagues
                var leaguesResponse = await _apiClient.GetLeaguesAsync();
                if (leaguesResponse.IsSuccess && leaguesResponse.Data != null)
                {
                    _collectiveData.Leagues = leaguesResponse.Data;
                }

                // Load currency items for each category
                foreach (var category in _collectiveData.Categories.CurrencyCategories)
                {
                    var currencyResponse = await _apiClient.GetCurrencyItemsAsync(category.ApiId, league: league, perPage: 1000);
                    if (currencyResponse.IsSuccess && currencyResponse.Data != null)
                    {
                        _collectiveData.CurrencyItems[category.ApiId] = currencyResponse.Data;
                        Logger.LogDebug($"Loaded {currencyResponse.Data.Items.Count} currency items for category '{category.ApiId}'");
                    }
                    
                    // Small delay to be nice to the API
                    await Task.Delay(100);
                }

                // Load unique items for each category
                foreach (var category in _collectiveData.Categories.UniqueCategories)
                {
                    var uniqueResponse = await _apiClient.GetUniqueItemsAsync(category.ApiId, league: league, perPage: 1000);
                    if (uniqueResponse.IsSuccess && uniqueResponse.Data != null)
                    {
                        _collectiveData.UniqueItems[category.ApiId] = uniqueResponse.Data;
                        Logger.LogDebug($"Loaded {uniqueResponse.Data.Items.Count} unique items for category '{category.ApiId}'");
                    }
                    
                    // Small delay to be nice to the API
                    await Task.Delay(100);
                }

                _collectiveData.LastUpdateTime = DateTime.UtcNow;
                Logger.LogInfo($"Successfully loaded all data for league: {league}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading data: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RefreshDataAsync(string league)
        {
            _collectiveData.Clear();
            _cacheService.Clear();
            return await LoadAllDataAsync(league);
        }
    }
}