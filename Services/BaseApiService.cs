using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using PortfelStudenta.Models;
namespace PortfelStudenta.Services;
public abstract class BaseApiService
{
    protected readonly HttpClient _httpClient;
    protected readonly ICacheService _cacheService;
    protected readonly IDatabaseService _databaseService;
    private const int MaxRetries = 3;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    protected BaseApiService(HttpClient httpClient, ICacheService cacheService, IDatabaseService databaseService)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout;
        _cacheService = cacheService;
        _databaseService = databaseService;
    }
    protected bool IsConnected()
    {
        var current = Connectivity.Current.NetworkAccess;
        return current == NetworkAccess.Internet;
    }
    protected async Task<T?> GetAsync<T>(string url, string cacheKey, TimeSpan? cacheDuration = null) where T : class
    {
        var cached = _cacheService.Get<T>(cacheKey);
        if (cached != null)
            return cached;
        if (!IsConnected())
        {
            throw new InvalidOperationException("Brak poL�ączenia z internetem. SprawdLs poL�ączenie sieciowe.");
        }
        Exception? lastException = null;
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<T>(content);
                    if (result != null)
                    {
                        _cacheService.Set(cacheKey, result, cacheDuration ?? TimeSpan.FromMinutes(1));
                    }
                    return result;
                }
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Debug.WriteLine($"[API] Nie znaleziono zasobu: {url}");
                    throw new InvalidOperationException("Nie znaleziono żądanego zasobu.");
                }
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                    continue;
                }
                lastException = new HttpRequestException($"HTTP Error: {response.StatusCode}");
            }
            catch (TaskCanceledException)
            {
                lastException = new TimeoutException("Przekroczono limit czasu LLądania. SprAlbuj ponownie.");
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"BL�ąd parsowania odpowiedzi: {ex.Message}");
            }
            if (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelay * attempt);
            }
        }
        throw lastException ?? new InvalidOperationException("Nieznany bL�ąd podczas pobierania danych.");
    }
    protected T? GetFromCache<T>(string cacheKey) where T : class
    {
        return _cacheService.Get<T>(cacheKey);
    }
    protected async Task SavePriceHistoryAsync(AssetPrice price)
    {
        try
        {
            var priceHistory = new PriceHistory
            {
                AssetType = price.AssetType,
                Symbol = price.Symbol,
                PricePln = price.PricePln,
                PriceOriginal = price.PriceOriginal,
                OriginalCurrency = price.OriginalCurrency,
                Timestamp = DateTime.UtcNow,
                Source = price.Source
            };
            await _databaseService.SavePriceHistoryAsync(priceHistory);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BaseApiService] Błąd zapisu historii cen: {ex.Message}");
        }
    }
}