using Newtonsoft.Json;
using PortfelStudenta.Models;
namespace PortfelStudenta.Services;
public interface INbpApiService
{
    Task<AssetPrice?> GetCurrencyRateAsync(string currencyCode);
    Task<List<AssetPrice>> GetAllCurrencyRatesAsync();
    Task<List<AssetPrice>> GetAllAvailableCurrenciesAsync();
    Task<decimal> GetUsdPlnRateAsync();
    Task<decimal> GetEurPlnRateAsync();
    Task<decimal> GetGbpPlnRateAsync();
    Task<decimal> GetJpyPlnRateAsync();
    Task<decimal> GetHkdPlnRateAsync();
    Task<decimal> GetKrwPlnRateAsync();
}
public class NbpApiService : BaseApiService, INbpApiService
{
    private const string BaseUrl = "https://api.nbp.pl/api/exchangerates/rates/a/";
    private const string TableAUrl = "https://api.nbp.pl/api/exchangerates/tables/a/";
    private static readonly string[] SupportedCurrencies = { "EUR", "USD", "GBP", "CHF" };
    public NbpApiService(HttpClient httpClient, ICacheService cacheService, IDatabaseService databaseService)
        : base(httpClient, cacheService, databaseService)
    {
    }
    public async Task<AssetPrice?> GetCurrencyRateAsync(string currencyCode)
    {
        var code = currencyCode.ToUpper();
        var url = $"{BaseUrl}{code}/";
        var cacheKey = $"nbp_{code}";
        try
        {
            var response = await GetAsync<NbpResponse>(url, cacheKey, TimeSpan.FromMinutes(2));
            if (response?.Rates?.FirstOrDefault() is { } rate)
            {
                var assetPrice = new AssetPrice
                {
                    AssetType = AssetType.CURRENCY,
                    Symbol = code,
                    Name = GetCurrencyName(code),
                    PricePln = (decimal)rate.Mid,
                    PriceOriginal = 1,
                    OriginalCurrency = code,
                    Source = "NBP",
                    LastUpdated = DateTime.UtcNow,
                    IsFromCache = false
                };
                await SavePriceHistoryAsync(assetPrice);
                return assetPrice;
            }
        }
        catch (Exception)
        {
            var cached = GetFromCache<NbpResponse>(cacheKey);
            if (cached?.Rates?.FirstOrDefault() is { } cachedRate)
            {
                return new AssetPrice
                {
                    AssetType = AssetType.CURRENCY,
                    Symbol = code,
                    Name = GetCurrencyName(code),
                    PricePln = (decimal)cachedRate.Mid,
                    Source = "NBP",
                    IsFromCache = true
                };
            }
            throw;
        }
        return null;
    }
    public async Task<List<AssetPrice>> GetAllCurrencyRatesAsync()
    {
        var results = new List<AssetPrice>();
        var tasks = SupportedCurrencies.Select(async code =>
        {
            try
            {
                var rate = await GetCurrencyRateAsync(code);
                if (rate != null)
                    return rate;
            }
            catch (Exception)
            {
            }
            return null;
        });
        var rates = await Task.WhenAll(tasks);
        results.AddRange(rates.Where(r => r != null)!);
        return results;
    }
    public async Task<List<AssetPrice>> GetAllAvailableCurrenciesAsync()
    {
        var cacheKey = "nbp_table_a";
        var results = new List<AssetPrice>();
        try
        {
            var response = await GetAsync<List<NbpTableResponse>>(TableAUrl, cacheKey, TimeSpan.FromMinutes(5));
            if (response?.FirstOrDefault()?.Rates is { } rates)
            {
                foreach (var rate in rates)
                {
                    if (SupportedCurrencies.Contains(rate.Code.ToUpper()))
                        continue;
                    results.Add(new AssetPrice
                    {
                        AssetType = AssetType.CURRENCY,
                        Symbol = rate.Code,
                        Name = rate.Currency,
                        PricePln = (decimal)rate.Mid,
                        PriceOriginal = 1,
                        OriginalCurrency = rate.Code,
                        Source = "NBP",
                        LastUpdated = DateTime.UtcNow,
                        IsFromCache = false
                    });
                }
            }
        }
        catch (Exception)
        {
            var cached = GetFromCache<List<NbpTableResponse>>(cacheKey);
            if (cached?.FirstOrDefault()?.Rates is { } cachedRates)
            {
                foreach (var rate in cachedRates)
                {
                    if (SupportedCurrencies.Contains(rate.Code.ToUpper()))
                        continue;
                    results.Add(new AssetPrice
                    {
                        AssetType = AssetType.CURRENCY,
                        Symbol = rate.Code,
                        Name = rate.Currency,
                        PricePln = (decimal)rate.Mid,
                        Source = "NBP",
                        IsFromCache = true
                    });
                }
            }
            else
            {
                throw;
            }
        }
        return results.OrderBy(c => c.Name).ToList();
    }
    public async Task<decimal> GetUsdPlnRateAsync()
    {
        var rate = await GetCurrencyRateAsync("USD");
        return rate?.PricePln ?? 4.0m;
    }
    public async Task<decimal> GetEurPlnRateAsync()
    {
        var rate = await GetCurrencyRateAsync("EUR");
        return rate?.PricePln ?? 4.3m;
    }
    public async Task<decimal> GetGbpPlnRateAsync()
    {
        var rate = await GetCurrencyRateAsync("GBP");
        return rate?.PricePln ?? 5.0m;
    }
    public async Task<decimal> GetJpyPlnRateAsync()
    {
        var rate = await GetCurrencyRateAsync("JPY");
        return rate?.PricePln ?? 0.027m;
    }
    public async Task<decimal> GetHkdPlnRateAsync()
    {
        var rate = await GetCurrencyRateAsync("HKD");
        return rate?.PricePln ?? 0.51m;
    }
    public async Task<decimal> GetKrwPlnRateAsync()
    {
        var rate = await GetCurrencyRateAsync("KRW");
        return rate?.PricePln ?? 0.003m;
    }
    private static string GetCurrencyName(string code) => code switch
    {
        "EUR" => "Euro",
        "USD" => "Dolar amerykański",
        "GBP" => "Funt brytyjski",
        "CHF" => "Frank szwajcarski",
        _ => code
    };
    private class NbpResponse
    {
        [JsonProperty("table")]
        public string Table { get; set; } = string.Empty;
        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        [JsonProperty("rates")]
        public List<NbpRate>? Rates { get; set; }
    }
    private class NbpRate
    {
        [JsonProperty("no")]
        public string No { get; set; } = string.Empty;
        [JsonProperty("effectiveDate")]
        public string EffectiveDate { get; set; } = string.Empty;
        [JsonProperty("mid")]
        public double Mid { get; set; }
    }
    private class NbpTableResponse
    {
        [JsonProperty("table")]
        public string Table { get; set; } = string.Empty;
        [JsonProperty("no")]
        public string No { get; set; } = string.Empty;
        [JsonProperty("effectiveDate")]
        public string EffectiveDate { get; set; } = string.Empty;
        [JsonProperty("rates")]
        public List<NbpTableRate>? Rates { get; set; }
    }
    private class NbpTableRate
    {
        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        [JsonProperty("mid")]
        public double Mid { get; set; }
    }
}