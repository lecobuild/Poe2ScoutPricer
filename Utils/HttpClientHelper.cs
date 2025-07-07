// Utils/HttpClientHelper.cs
using System.Net.Http;
using System.Text.Json;

namespace Poe2ScoutPricer.Utils
{
    public static class HttpClientHelper
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task<T?> GetJsonAsync<T>(this HttpClient httpClient, string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to get JSON from {url}: {ex.Message}");
                throw;
            }
        }

        public static async Task<string> GetStringAsync(this HttpClient httpClient, string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to get string from {url}: {ex.Message}");
                throw;
            }
        }

        public static HttpClient CreateHttpClient(TimeSpan? timeout = null)
        {
            var client = new HttpClient();
            
            if (timeout.HasValue)
            {
                client.Timeout = timeout.Value;
            }
            else
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            }

            client.DefaultRequestHeaders.Add("User-Agent", "Poe2ScoutPricer/1.0");
            
            return client;
        }
    }
}