// API/Poe2ScoutApiClient.cs
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Poe2ScoutPricer.API.Models;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.API
{
    public class Poe2ScoutApiClient : IPoe2ScoutApi, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _baseUrl = "https://poe2scout.com/api";
        private bool _disposed = false;

        public Poe2ScoutApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Poe2ScoutPricer/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<ApiResponse<CategoryResponse>> GetCategoriesAsync()
        {
            return await MakeRequestAsync<CategoryResponse>("/items/categories");
        }

        public async Task<ApiResponse<UniqueItemsResponse>> GetUniqueItemsAsync(
            string category, 
            string search = "", 
            int page = 1, 
            int perPage = 25, 
            string league = "Standard")
        {
            var queryParams = new Dictionary<string, string>
            {
                ["search"] = search,
                ["page"] = page.ToString(),
                ["perPage"] = perPage.ToString(),
                ["league"] = league
            };

            var url = $"/items/unique/{WebUtility.UrlEncode(category)}" + BuildQueryString(queryParams);
            return await MakeRequestAsync<UniqueItemsResponse>(url);
        }

        public async Task<ApiResponse<CurrencyItemsResponse>> GetCurrencyItemsAsync(
            string category, 
            string search = "", 
            int page = 1, 
            int perPage = 25, 
            string league = "Standard")
        {
            var queryParams = new Dictionary<string, string>
            {
                ["search"] = search,
                ["page"] = page.ToString(),
                ["perPage"] = perPage.ToString(),
                ["league"] = league
            };

            var url = $"/items/currency/{WebUtility.UrlEncode(category)}" + BuildQueryString(queryParams);
            return await MakeRequestAsync<CurrencyItemsResponse>(url);
        }

        public async Task<ApiResponse<List<League>>> GetLeaguesAsync()
        {
            return await MakeRequestAsync<List<League>>("/leagues");
        }

        public async Task<ApiResponse<UniqueBaseItemsResponse>> GetUniqueBaseItemsAsync(
            string search = "", 
            bool showUnChanceable = false, 
            string sortedBy = "price", 
            int page = 1, 
            int perPage = 25, 
            string league = "Standard")
        {
            var queryParams = new Dictionary<string, string>
            {
                ["search"] = search,
                ["showUnChanceable"] = showUnChanceable.ToString().ToLower(),
                ["sortedBy"] = sortedBy,
                ["page"] = page.ToString(),
                ["perPage"] = perPage.ToString(),
                ["league"] = league
            };

            var url = "/items/uniqueBaseItems" + BuildQueryString(queryParams);
            return await MakeRequestAsync<UniqueBaseItemsResponse>(url);
        }

        public async Task<ApiResponse<UniquesByBaseNameResponse>> GetUniquesByBaseNameAsync(string baseName, string league)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["league"] = league
            };

            var url = $"/items/uniquesByBaseName/{WebUtility.UrlEncode(baseName)}" + BuildQueryString(queryParams);
            return await MakeRequestAsync<UniquesByBaseNameResponse>(url);
        }

        public async Task<ApiResponse<CurrencyItemByIdResponse>> GetCurrencyItemByIdAsync(string apiId, string league)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["league"] = league
            };

            var url = $"/items/currencyById/{WebUtility.UrlEncode(apiId)}" + BuildQueryString(queryParams);
            return await MakeRequestAsync<CurrencyItemByIdResponse>(url);
        }

        public async Task<ApiResponse<string>> GetItemHistoryAsync(int itemId, string league, int logCount)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["league"] = league,
                ["logCount"] = logCount.ToString()
            };

            var url = $"/items/{itemId}/history" + BuildQueryString(queryParams);
            return await MakeRequestAsync<string>(url);
        }

        private async Task<ApiResponse<T>> MakeRequestAsync<T>(string endpoint)
        {
            try
            {
                var url = _baseUrl + endpoint;
                Logger.LogDebug($"Making request to: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                Logger.LogDebug($"Response status: {response.StatusCode}");
                Logger.LogDebug($"Response content length: {content.Length}");

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return new ApiResponse<T>
                    {
                        IsSuccess = true,
                        Data = data,
                        StatusCode = (int)response.StatusCode
                    };
                }
                else
                {
                    var errorMessage = $"Request failed with status {response.StatusCode}";
                    
                    // Try to parse validation error
                    try
                    {
                        var validationError = JsonSerializer.Deserialize<ValidationError>(content, _jsonOptions);
                        if (validationError?.Detail?.Any() == true)
                        {
                            errorMessage += $": {string.Join(", ", validationError.Detail.Select(d => d.Message))}";
                        }
                    }
                    catch
                    {
                        errorMessage += $": {content}";
                    }

                    Logger.LogError($"API request failed: {errorMessage}");

                    return new ApiResponse<T>
                    {
                        IsSuccess = false,
                        ErrorMessage = errorMessage,
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"Network error: {ex.Message}";
                Logger.LogError($"HTTP request exception: {ex}");
                
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
            catch (TaskCanceledException ex)
            {
                var errorMessage = ex.InnerException is TimeoutException ? "Request timeout" : "Request cancelled";
                Logger.LogError($"Request timeout/cancelled: {ex}");
                
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
            catch (JsonException ex)
            {
                var errorMessage = $"Failed to parse response: {ex.Message}";
                Logger.LogError($"JSON parsing error: {ex}");
                
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error: {ex.Message}";
                Logger.LogError($"Unexpected error in API client: {ex}");
                
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
        }

        private static string BuildQueryString(Dictionary<string, string> parameters)
        {
            if (parameters == null || !parameters.Any())
                return string.Empty;

            var filteredParams = parameters
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}");

            return "?" + string.Join("&", filteredParams);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}