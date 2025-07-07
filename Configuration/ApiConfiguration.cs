// Configuration/ApiConfiguration.cs
namespace Poe2ScoutPricer.Configuration
{
    public static class ApiConfiguration
    {
        public const string BaseUrl = "https://poe2scout.com/api";
        public const string UserAgent = "Poe2ScoutPricer/1.0";
        public const int DefaultTimeout = 30;
        public const int DefaultPerPage = 1000;
        public const int MaxPerPage = 10000;
        public const int MinPerPage = 1;
        
        // Rate limiting
        public const int RequestDelayMs = 100;
        public const int MaxConcurrentRequests = 3;
        
        // Cache settings
        public const int DefaultCacheHours = 2;
        public const int MaxCacheHours = 24;
        
        // API endpoints
        public static class Endpoints
        {
            public const string Categories = "/items/categories";
            public const string Leagues = "/leagues";
            public const string UniqueItems = "/items/unique/{0}";
            public const string CurrencyItems = "/items/currency/{0}";
            public const string UniqueBaseItems = "/items/uniqueBaseItems";
            public const string UniquesByBaseName = "/items/uniquesByBaseName/{0}";
            public const string CurrencyById = "/items/currencyById/{0}";
            public const string ItemHistory = "/items/{0}/history";
        }
        
        // Default leagues (will be populated from API)
        public static readonly List<string> DefaultLeagues = new()
        {
            "Standard",
            "Hardcore"
        };
        
        // Supported sort options
        public static readonly List<string> SortOptions = new()
        {
            "price",
            "name", 
            "uniquePrice",
            "-price",
            "-uniquePrice", 
            "-name"
        };
    }
}