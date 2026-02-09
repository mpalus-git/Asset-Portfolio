using Newtonsoft.Json;
using PortfelStudenta.Models;
namespace PortfelStudenta.Services;
public interface ICoinCapApiService
{
    Task<AssetPrice?> GetCryptoAssetAsync(string assetId);
    Task<List<AssetPrice>> GetAllCryptoAssetsAsync();
    Task<List<AssetPrice>> GetTopCryptoAssetsAsync(int limit = 5);
    Task<List<CryptoKline>> GetKlinesAsync(string symbol, string interval, int limit = 100);
    Task<AssetPrice?> GetCryptoAssetWithDetailsAsync(string assetId);
}
public class CoinCapApiService : BaseApiService, ICoinCapApiService
{
    private const string BaseUrl = "https://api.binance.com/api/v3";
    private readonly INbpApiService _nbpApiService;
    private decimal? _cachedUsdPlnRate;
    private decimal _lastKnownUsdPlnRate = 4.0m;
    private DateTime _rateExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private static readonly List<(string Symbol, string Name)> TopCryptos = new()
    {
        ("BTCUSDT", "Bitcoin"),
        ("ETHUSDT", "Ethereum"),
        ("BNBUSDT", "BNB"),
        ("XRPUSDT", "XRP"),
        ("SOLUSDT", "Solana")
    };
    public CoinCapApiService(
        HttpClient httpClient,
        ICacheService cacheService,
        IDatabaseService databaseService,
        INbpApiService nbpApiService)
        : base(httpClient, cacheService, databaseService)
    {
        _nbpApiService = nbpApiService;
    }
    private async Task<decimal> GetCachedUsdPlnRateAsync()
    {
        if (_cachedUsdPlnRate.HasValue && DateTime.UtcNow < _rateExpiresAt)
            return _cachedUsdPlnRate.Value;
        await _rateLock.WaitAsync();
        try
        {
            if (_cachedUsdPlnRate.HasValue && DateTime.UtcNow < _rateExpiresAt)
                return _cachedUsdPlnRate.Value;
            try
            {
                _cachedUsdPlnRate = await _nbpApiService.GetUsdPlnRateAsync();
                _lastKnownUsdPlnRate = _cachedUsdPlnRate.Value;
            }
            catch
            {
                _cachedUsdPlnRate = _lastKnownUsdPlnRate;
            }
            _rateExpiresAt = DateTime.UtcNow.AddMinutes(5);
            return _cachedUsdPlnRate.Value;
        }
        finally
        {
            _rateLock.Release();
        }
    }
    public async Task<AssetPrice?> GetCryptoAssetAsync(string assetId)
    {
        var symbol = assetId.ToUpper();
        if (!symbol.EndsWith("USDT"))
            symbol += "USDT";
        var url = $"{BaseUrl}/ticker/24hr?symbol={symbol}";
        var cacheKey = $"binance_{symbol}";
        try
        {
            var response = await GetAsync<BinanceTickerResponse>(url, cacheKey, TimeSpan.FromMinutes(1));
            if (response != null)
            {
                var usdPlnRate = await GetCachedUsdPlnRateAsync();
                return await ConvertToAssetPriceAsync(response, usdPlnRate);
            }
        }
        catch (Exception)
        {
            var cached = GetFromCache<BinanceTickerResponse>(cacheKey);
            if (cached != null)
            {
                var priceUsd = decimal.Parse(cached.LastPrice ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                var baseSymbol = symbol.Replace("USDT", "");
                return new AssetPrice
                {
                    AssetType = AssetType.CRYPTO,
                    Symbol = baseSymbol,
                    Name = GetCryptoName(baseSymbol),
                    PricePln = priceUsd * _lastKnownUsdPlnRate,
                    PriceOriginal = priceUsd,
                    OriginalCurrency = "USD",
                    Source = "Binance",
                    IsFromCache = true
                };
            }
            throw;
        }
        return null;
    }
    public async Task<AssetPrice?> GetCryptoAssetWithDetailsAsync(string assetId)
    {
        var symbol = assetId.ToUpper();
        if (!symbol.EndsWith("USDT"))
            symbol += "USDT";
        var url = $"{BaseUrl}/ticker/24hr?symbol={symbol}";
        var cacheKey = $"binance_details_{symbol}";
        try
        {
            var response = await GetAsync<BinanceTickerResponse>(url, cacheKey, TimeSpan.FromMinutes(1));
            if (response != null)
            {
                var usdPlnRate = await GetCachedUsdPlnRateAsync();
                return await ConvertToAssetPriceWithDetailsAsync(response, usdPlnRate);
            }
        }
        catch (Exception)
        {
            var cached = GetFromCache<BinanceTickerResponse>(cacheKey);
            if (cached != null)
            {
                return await ConvertToAssetPriceWithDetailsAsync(cached, _lastKnownUsdPlnRate);
            }
            throw;
        }
        return null;
    }
    public async Task<List<CryptoKline>> GetKlinesAsync(string symbol, string interval, int limit = 100)
    {
        var binanceSymbol = symbol.ToUpper();
        if (!binanceSymbol.EndsWith("USDT"))
            binanceSymbol += "USDT";
        var url = $"{BaseUrl}/klines?symbol={binanceSymbol}&interval={interval}&limit={limit}";
        var cacheKey = $"binance_klines_{binanceSymbol}_{interval}_{limit}";
        try
        {
            var response = await GetAsync<List<List<object>>>(url, cacheKey, TimeSpan.FromMinutes(5));
            if (response != null)
            {
                return ParseKlines(response);
            }
        }
        catch (Exception)
        {
            var cached = GetFromCache<List<List<object>>>(cacheKey);
            if (cached != null)
            {
                return ParseKlines(cached);
            }
        }
        return new List<CryptoKline>();
    }
    private static List<CryptoKline> ParseKlines(List<List<object>> data)
    {
        return data.Select(k => new CryptoKline
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(k[0])).UtcDateTime,
            Open = decimal.Parse(k[1].ToString()!, System.Globalization.CultureInfo.InvariantCulture),
            High = decimal.Parse(k[2].ToString()!, System.Globalization.CultureInfo.InvariantCulture),
            Low = decimal.Parse(k[3].ToString()!, System.Globalization.CultureInfo.InvariantCulture),
            Close = decimal.Parse(k[4].ToString()!, System.Globalization.CultureInfo.InvariantCulture),
            Volume = decimal.Parse(k[5].ToString()!, System.Globalization.CultureInfo.InvariantCulture),
            CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(k[6])).UtcDateTime
        }).ToList();
    }
    public async Task<List<AssetPrice>> GetTopCryptoAssetsAsync(int limit = 5)
    {
        var results = new List<AssetPrice>();
        var symbols = TopCryptos.Take(limit).ToList();
        var usdPlnRate = await GetCachedUsdPlnRateAsync();
        foreach (var (symbol, name) in symbols)
        {
            var url = $"{BaseUrl}/ticker/24hr?symbol={symbol}";
            var cacheKey = $"binance_{symbol}";
            try
            {
                var response = await GetAsync<BinanceTickerResponse>(url, cacheKey, TimeSpan.FromMinutes(1));
                if (response != null)
                {
                    var assetPrice = await ConvertToAssetPriceWithDetailsAsync(response, usdPlnRate, name);
                    if (assetPrice != null)
                    {
                        results.Add(assetPrice);
                    }
                }
            }
            catch (Exception)
            {
                var cached = GetFromCache<BinanceTickerResponse>(cacheKey);
                if (cached != null)
                {
                    var assetPrice = await ConvertToAssetPriceWithDetailsAsync(cached, usdPlnRate, name);
                    if (assetPrice != null)
                    {
                        assetPrice.IsFromCache = true;
                        results.Add(assetPrice);
                    }
                }
            }
            await Task.Delay(50);
        }
        return results;
    }
    public async Task<List<AssetPrice>> GetAllCryptoAssetsAsync()
    {
        return await GetTopCryptoAssetsAsync(5);
    }
    private async Task<AssetPrice?> ConvertToAssetPriceAsync(BinanceTickerResponse data, decimal usdPlnRate, string? overrideName = null)
    {
        try
        {
            var priceUsd = decimal.Parse(data.LastPrice ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var priceChangePercent = decimal.Parse(data.PriceChangePercent ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var baseSymbol = data.Symbol?.Replace("USDT", "") ?? "?";
            var assetPrice = new AssetPrice
            {
                AssetType = AssetType.CRYPTO,
                Symbol = baseSymbol,
                Name = overrideName ?? GetCryptoName(baseSymbol),
                PricePln = priceUsd * usdPlnRate,
                PriceOriginal = priceUsd,
                OriginalCurrency = "USD",
                ChangePercent24h = priceChangePercent,
                Source = "Binance",
                LastUpdated = DateTime.UtcNow,
                IsFromCache = false
            };
            await SavePriceHistoryAsync(assetPrice);
            return assetPrice;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private async Task<AssetPrice?> ConvertToAssetPriceWithDetailsAsync(BinanceTickerResponse data, decimal usdPlnRate, string? overrideName = null)
    {
        try
        {
            var priceUsd = decimal.Parse(data.LastPrice ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var priceChangePercent = decimal.Parse(data.PriceChangePercent ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var baseSymbol = data.Symbol?.Replace("USDT", "") ?? "?";
            var assetPrice = new AssetPrice
            {
                AssetType = AssetType.CRYPTO,
                Symbol = baseSymbol,
                Name = overrideName ?? GetCryptoName(baseSymbol),
                PricePln = priceUsd * usdPlnRate,
                PriceOriginal = priceUsd,
                OriginalCurrency = "USD",
                ChangePercent24h = priceChangePercent,
                Source = "Binance",
                LastUpdated = DateTime.UtcNow,
                IsFromCache = false,
                HighPrice24h = ParseDecimalOrNull(data.HighPrice),
                LowPrice24h = ParseDecimalOrNull(data.LowPrice),
                PriceChange24h = ParseDecimalOrNull(data.PriceChange),
                Volume24h = ParseDecimalOrNull(data.Volume),
                QuoteVolume24h = ParseDecimalOrNull(data.QuoteVolume),
                OpenPrice = ParseDecimalOrNull(data.OpenPrice),
                TradeCount = data.Count
            };
            await SavePriceHistoryAsync(assetPrice);
            return assetPrice;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static decimal? ParseDecimalOrNull(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }
    private static string GetCryptoName(string symbol)
    {
        return symbol.ToUpper() switch
        {
            "BTC" => "Bitcoin",
            "ETH" => "Ethereum",
            "BNB" => "BNB",
            "XRP" => "XRP",
            "SOL" => "Solana",
            "ADA" => "Cardano",
            "DOGE" => "Dogecoin",
            "DOT" => "Polkadot",
            "MATIC" => "Polygon",
            "SHIB" => "Shiba Inu",
            _ => symbol.ToUpper()
        };
    }
    private class BinanceTickerResponse
    {
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
        [JsonProperty("lastPrice")]
        public string? LastPrice { get; set; }
        [JsonProperty("priceChange")]
        public string? PriceChange { get; set; }
        [JsonProperty("priceChangePercent")]
        public string? PriceChangePercent { get; set; }
        [JsonProperty("highPrice")]
        public string? HighPrice { get; set; }
        [JsonProperty("lowPrice")]
        public string? LowPrice { get; set; }
        [JsonProperty("volume")]
        public string? Volume { get; set; }
        [JsonProperty("quoteVolume")]
        public string? QuoteVolume { get; set; }
        [JsonProperty("openPrice")]
        public string? OpenPrice { get; set; }
        [JsonProperty("count")]
        public long? Count { get; set; }
    }
}