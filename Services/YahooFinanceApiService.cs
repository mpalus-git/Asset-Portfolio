using Newtonsoft.Json;
using PortfelStudenta.Models;
namespace PortfelStudenta.Services;
public class StockKline
{
    public DateTime OpenTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
public interface IYahooFinanceApiService
{
    Task<AssetPrice?> GetStockPriceAsync(string symbol);
    Task<List<AssetPrice>> GetAllStockPricesAsync();
    Task<List<AssetPrice>> GetPolishStocksAsync();
    Task<List<AssetPrice>> GetUSStocksAsync();
    Task<List<AssetPrice>> GetEuropeanStocksAsync();
    Task<List<AssetPrice>> GetAsianStocksAsync();
    Task<AssetPrice?> SearchStockAsync(string query);
    Task<List<StockKline>> GetStockHistoryAsync(string symbol, string interval, int limit);
}
public class YahooFinanceApiService : BaseApiService, IYahooFinanceApiService
{
    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart/";
    private const string QuoteUrl = "https://query1.finance.yahoo.com/v7/finance/quote";
    private readonly INbpApiService _nbpApiService;
    private record StockInfo(string Name, string Currency);
    private static readonly Dictionary<string, StockInfo> PolishStocks = new()
    {
        { "PKO.WA", new StockInfo("PKO BP", "PLN") },
        { "PKN.WA", new StockInfo("ORLEN", "PLN") },
        { "ALE.WA", new StockInfo("Allegro", "PLN") },
        { "KGH.WA", new StockInfo("KGHM", "PLN") },
        { "DNP.WA", new StockInfo("Dino Polska", "PLN") }
    };
    private static readonly Dictionary<string, StockInfo> USStocks = new()
    {
        { "NVDA", new StockInfo("Nvidia", "USD") },
        { "AAPL", new StockInfo("Apple", "USD") },
        { "MSFT", new StockInfo("Microsoft", "USD") },
        { "GOOGL", new StockInfo("Alphabet", "USD") },
        { "AMZN", new StockInfo("Amazon", "USD") }
    };
    private static readonly Dictionary<string, StockInfo> EuropeanStocks = new()
    {
        { "ASML.AS", new StockInfo("ASML", "EUR") },
        { "MC.PA", new StockInfo("LVMH", "EUR") },
        { "NVO", new StockInfo("Novo Nordisk", "USD") },
        { "SAP.DE", new StockInfo("SAP", "EUR") },
        { "TTE", new StockInfo("TotalEnergies", "USD") }
    };
    private static readonly Dictionary<string, StockInfo> AsianStocks = new()
    {
        { "TSM", new StockInfo("TSMC", "USD") },
        { "TCEHY", new StockInfo("Tencent", "USD") },
        { "005930.KS", new StockInfo("Samsung Electronics", "KRW") },
        { "BABA", new StockInfo("Alibaba", "USD") },
        { "7203.T", new StockInfo("Toyota", "JPY") }
    };
    private static readonly Dictionary<string, StockInfo> SupportedStocks = new(
        PolishStocks
        .Concat(USStocks)
        .Concat(EuropeanStocks)
        .Concat(AsianStocks)
    );
    public YahooFinanceApiService(
        HttpClient httpClient,
        ICacheService cacheService,
        IDatabaseService databaseService,
        INbpApiService nbpApiService)
        : base(httpClient, cacheService, databaseService)
    {
        _nbpApiService = nbpApiService;
    }
    public async Task<List<AssetPrice>> GetPolishStocksAsync()
    {
        return await GetStocksFromListAsync(PolishStocks);
    }
    public async Task<List<AssetPrice>> GetUSStocksAsync()
    {
        return await GetStocksFromListAsync(USStocks);
    }
    public async Task<List<AssetPrice>> GetEuropeanStocksAsync()
    {
        return await GetStocksFromListAsync(EuropeanStocks);
    }
    public async Task<List<AssetPrice>> GetAsianStocksAsync()
    {
        return await GetStocksFromListAsync(AsianStocks);
    }
    private async Task<List<AssetPrice>> GetStocksFromListAsync(Dictionary<string, StockInfo> stocks)
    {
        var results = new List<AssetPrice>();
        foreach (var stock in stocks)
        {
            try
            {
                var price = await GetStockPriceAsync(stock.Key);
                if (price != null)
                    results.Add(price);
            }
            catch (Exception)
            {
            }
            await Task.Delay(250);
        }
        return results;
    }
    public async Task<AssetPrice?> GetStockPriceAsync(string symbol)
    {
        var sym = symbol.ToUpper();
        var url = $"{BaseUrl}{sym}?interval=1d&range=1d";
        var cacheKey = $"yahoo_{sym}";
        try
        {
            var response = await GetWithHeadersAsync<YahooResponse>(url, cacheKey, TimeSpan.FromMinutes(5));
            var result = response?.Chart?.Result?.FirstOrDefault();
            var meta = result?.Meta;
            if (meta != null)
            {
                var price = (decimal)(meta.RegularMarketPrice ?? 0);
                var previousClose = (decimal)(meta.ChartPreviousClose ?? meta.PreviousClose ?? (double)price);
                var currency = meta.Currency ?? "USD";
                decimal? changePercent = previousClose > 0
                    ? ((price - previousClose) / previousClose) * 100
                    : null;
                decimal pricePln = price;
                if (currency != "PLN")
                {
                    var rate = await GetCurrencyRateAsync(currency);
                    pricePln = price * rate;
                }
                var stockInfo = SupportedStocks.GetValueOrDefault(sym) ?? new StockInfo(sym, currency);
                var assetPrice = new AssetPrice
                {
                    AssetType = AssetType.STOCK,
                    Symbol = sym,
                    Name = stockInfo.Name,
                    PricePln = pricePln,
                    PriceOriginal = price,
                    OriginalCurrency = currency,
                    ChangePercent24h = changePercent,
                    Source = "Yahoo Finance",
                    LastUpdated = DateTime.UtcNow,
                    IsFromCache = false
                };
                await SavePriceHistoryAsync(assetPrice);
                return assetPrice;
            }
        }
        catch (Exception)
        {
            var cached = GetFromCache<YahooResponse>(cacheKey);
            var cachedResult = cached?.Chart?.Result?.FirstOrDefault();
            var cachedMeta = cachedResult?.Meta;
            if (cachedMeta != null)
            {
                var price = (decimal)(cachedMeta.RegularMarketPrice ?? 0);
                var currency = cachedMeta.Currency ?? "USD";
                var stockInfo = SupportedStocks.GetValueOrDefault(sym) ?? new StockInfo(sym, currency);
                decimal pricePln = price;
                if (currency != "PLN")
                {
                    pricePln = price * GetFallbackRate(currency);
                }
                return new AssetPrice
                {
                    AssetType = AssetType.STOCK,
                    Symbol = sym,
                    Name = stockInfo.Name,
                    PricePln = pricePln,
                    PriceOriginal = price,
                    OriginalCurrency = currency,
                    Source = "Yahoo Finance",
                    IsFromCache = true
                };
            }
            return null;
        }
        return null;
    }
    private async Task<T?> GetWithHeadersAsync<T>(string url, string cacheKey, TimeSpan cacheDuration) where T : class
    {
        var cached = _cacheService.Get<T>(cacheKey);
        if (cached != null)
            return cached;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(content);
                if (result != null)
                {
                    _cacheService.Set(cacheKey, result, cacheDuration);
                }
                return result;
            }
        }
        catch (Exception)
        {
        }
        return null;
    }
    private static decimal GetFallbackRate(string currency)
    {
        return currency.ToUpper() switch
        {
            "PLN" => 1m,
            "USD" => 4.0m,
            "EUR" => 4.3m,
            "GBP" => 5.0m,
            "JPY" => 0.027m,
            "HKD" => 0.51m,
            "KRW" => 0.003m,
            _ => 4.0m
        };
    }
    private async Task<decimal> GetCurrencyRateAsync(string currency)
    {
        try
        {
            return currency.ToUpper() switch
            {
                "PLN" => 1m,
                "USD" => await _nbpApiService.GetUsdPlnRateAsync(),
                "EUR" => await _nbpApiService.GetEurPlnRateAsync(),
                "GBP" => await _nbpApiService.GetGbpPlnRateAsync(),
                "JPY" => await _nbpApiService.GetJpyPlnRateAsync(),
                "HKD" => await _nbpApiService.GetHkdPlnRateAsync(),
                "KRW" => await _nbpApiService.GetKrwPlnRateAsync(),
                _ => await _nbpApiService.GetUsdPlnRateAsync()
            };
        }
        catch
        {
            return GetFallbackRate(currency);
        }
    }
    public async Task<List<AssetPrice>> GetAllStockPricesAsync()
    {
        var results = new List<AssetPrice>();
        foreach (var stock in SupportedStocks)
        {
            try
            {
                var price = await GetStockPriceAsync(stock.Key);
                if (price != null)
                    results.Add(price);
            }
            catch (Exception)
            {
            }
            await Task.Delay(250);
        }
        return results;
    }
    public async Task<AssetPrice?> SearchStockAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;
        var symbol = query.Trim().ToUpper();
        var summaryUrl = $"https://query1.finance.yahoo.com/v11/finance/quoteSummary/{symbol}?modules=assetProfile,defaultKeyStatistics,financialData";
        var summaryCacheKey = $"yahoo_summary_{symbol}";
        decimal? marketCap = null;
        decimal? peRatio = null;
        string? companyName = null;
        string? exchange = null;
        decimal? regularMarketPrice = null;
        string? currency = null;
        decimal? previousClose = null;
        decimal? dayHigh = null;
        decimal? dayLow = null;
        decimal? volume = null;
        decimal? fiftyTwoWeekHigh = null;
        decimal? fiftyTwoWeekLow = null;
        try
        {
            var summaryResponse = await GetWithHeadersAsync<YahooSummaryResponse>(summaryUrl, summaryCacheKey, TimeSpan.FromMinutes(5));
            var summaryResult = summaryResponse?.QuoteSummary?.Result?.FirstOrDefault();
            if (summaryResult != null)
            {
                var priceData = summaryResult.Price;
                var summaryDetail = summaryResult.SummaryDetail;
                var keyStats = summaryResult.DefaultKeyStatistics;
                if (priceData != null)
                {
                    companyName = priceData.LongName ?? priceData.ShortName;
                    exchange = priceData.ExchangeName ?? priceData.Exchange;
                    regularMarketPrice = GetRawValue(priceData.RegularMarketPrice);
                    currency = priceData.Currency;
                    marketCap = GetRawValue(priceData.MarketCap);
                }
                if (summaryDetail != null)
                {
                    previousClose = GetRawValue(summaryDetail.PreviousClose);
                    dayHigh = GetRawValue(summaryDetail.DayHigh);
                    dayLow = GetRawValue(summaryDetail.DayLow);
                    volume = GetRawValue(summaryDetail.Volume);
                    fiftyTwoWeekHigh = GetRawValue(summaryDetail.FiftyTwoWeekHigh);
                    fiftyTwoWeekLow = GetRawValue(summaryDetail.FiftyTwoWeekLow);
                    peRatio = GetRawValue(summaryDetail.TrailingPE);
                }
                if (keyStats != null && peRatio == null)
                {
                    peRatio = GetRawValue(keyStats.TrailingPE) ?? GetRawValue(keyStats.ForwardPE);
                }
            }
        }
        catch
        {
        }
        if (regularMarketPrice == null || regularMarketPrice == 0)
        {
            var chartUrl = $"{BaseUrl}{symbol}?interval=1d&range=5d&includePrePost=false";
            var chartCacheKey = $"yahoo_search_{symbol}";
            try
            {
                var response = await GetWithHeadersAsync<YahooResponse>(chartUrl, chartCacheKey, TimeSpan.FromMinutes(2));
                var result = response?.Chart?.Result?.FirstOrDefault();
                var meta = result?.Meta;
                if (meta != null && meta.RegularMarketPrice != null && meta.RegularMarketPrice > 0)
                {
                    regularMarketPrice = (decimal)(meta.RegularMarketPrice ?? 0);
                    previousClose ??= (decimal?)(meta.ChartPreviousClose ?? meta.PreviousClose);
                    currency ??= meta.Currency ?? "USD";
                    companyName ??= meta.LongName ?? meta.ShortName;
                    exchange ??= meta.ExchangeName ?? meta.Exchange;
                    fiftyTwoWeekHigh ??= (decimal?)(meta.FiftyTwoWeekHigh);
                    fiftyTwoWeekLow ??= (decimal?)(meta.FiftyTwoWeekLow);
                    var quotes = result?.Indicators?.Quote?.FirstOrDefault();
                    if (quotes != null)
                    {
                        var highs = quotes.High?.Where(h => h.HasValue).Select(h => h!.Value).ToList();
                        var lows = quotes.Low?.Where(l => l.HasValue).Select(l => l!.Value).ToList();
                        var volumes = quotes.Volume?.Where(v => v.HasValue).Select(v => v!.Value).ToList();
                        if (highs?.Any() == true && dayHigh == null)
                            dayHigh = (decimal)highs.Last();
                        if (lows?.Any() == true && dayLow == null)
                            dayLow = (decimal)lows.Last();
                        if (volumes?.Any() == true && volume == null)
                            volume = volumes.Last();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        if (regularMarketPrice == null || regularMarketPrice == 0)
            return null;
        var price = regularMarketPrice.Value;
        currency ??= "USD";
        previousClose ??= price;
        decimal? changePercent = previousClose > 0
            ? ((price - previousClose.Value) / previousClose.Value) * 100
            : null;
        decimal? priceChange = price - previousClose;
        decimal pricePln = price;
        if (currency != "PLN")
        {
            var rate = await GetCurrencyRateAsync(currency);
            pricePln = price * rate;
        }
        var stockInfo = SupportedStocks.GetValueOrDefault(symbol);
        var name = stockInfo?.Name ?? companyName ?? symbol;
        var assetPrice = new AssetPrice
        {
            AssetType = AssetType.STOCK,
            Symbol = symbol,
            Name = name,
            PricePln = pricePln,
            PriceOriginal = price,
            OriginalCurrency = currency,
            ChangePercent24h = changePercent,
            PriceChange24h = priceChange,
            HighPrice24h = dayHigh,
            LowPrice24h = dayLow,
            Volume24h = volume,
            QuoteVolume24h = volume.HasValue ? volume * price : null,
            FiftyTwoWeekHigh = fiftyTwoWeekHigh,
            FiftyTwoWeekLow = fiftyTwoWeekLow,
            MarketCap = marketCap,
            PeRatio = peRatio,
            Exchange = exchange,
            Source = "Yahoo Finance",
            LastUpdated = DateTime.UtcNow,
            IsFromCache = false
        };
        return assetPrice;
    }
    private static decimal? GetRawValue(YahooRawValue? rawValue)
    {
        if (rawValue?.Raw == null)
            return null;
        return (decimal)rawValue.Raw.Value;
    }
    public async Task<List<StockKline>> GetStockHistoryAsync(string symbol, string interval, int limit)
    {
        var klines = new List<StockKline>();
        if (string.IsNullOrWhiteSpace(symbol))
            return klines;
        var sym = symbol.Trim().ToUpper();
        var (yahooInterval, range) = interval switch
        {
            "1h" => ("1h", "1d"),
            "4h" => ("1h", "5d"),
            "1d" => ("1d", "1mo"),
            "1w" => ("1wk", "1y"),
            _ => ("1d", "1mo")
        };
        var url = $"{BaseUrl}{sym}?interval={yahooInterval}&range={range}";
        var cacheKey = $"yahoo_history_{sym}_{yahooInterval}_{range}";
        try
        {
            var response = await GetWithHeadersAsync<YahooResponse>(url, cacheKey, TimeSpan.FromMinutes(5));
            var result = response?.Chart?.Result?.FirstOrDefault();
            if (result?.Timestamp != null && result.Indicators?.Quote?.FirstOrDefault() != null)
            {
                var timestamps = result.Timestamp;
                var quote = result.Indicators.Quote[0];
                for (int i = 0; i < timestamps.Count && i < limit; i++)
                {
                    if (quote.Close != null && i < quote.Close.Count && quote.Close[i].HasValue)
                    {
                        klines.Add(new StockKline
                        {
                            OpenTime = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).DateTime,
                            Open = (decimal)(quote.Open?[i] ?? 0),
                            High = (decimal)(quote.High?[i] ?? 0),
                            Low = (decimal)(quote.Low?[i] ?? 0),
                            Close = (decimal)(quote.Close[i] ?? 0),
                            Volume = (long)(quote.Volume?[i] ?? 0)
                        });
                    }
                }
                if (klines.Count > limit)
                {
                    klines = klines.TakeLast(limit).ToList();
                }
            }
        }
        catch (Exception)
        {
        }
        return klines;
    }
    private class YahooResponse
    {
        [JsonProperty("chart")]
        public YahooChart? Chart { get; set; }
    }
    private class YahooChart
    {
        [JsonProperty("result")]
        public List<YahooResult>? Result { get; set; }
        [JsonProperty("error")]
        public object? Error { get; set; }
    }
    private class YahooResult
    {
        [JsonProperty("meta")]
        public YahooMeta? Meta { get; set; }
        [JsonProperty("indicators")]
        public YahooIndicators? Indicators { get; set; }
        [JsonProperty("timestamp")]
        public List<long>? Timestamp { get; set; }
    }
    private class YahooMeta
    {
        [JsonProperty("currency")]
        public string? Currency { get; set; }
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
        [JsonProperty("shortName")]
        public string? ShortName { get; set; }
        [JsonProperty("longName")]
        public string? LongName { get; set; }
        [JsonProperty("exchangeName")]
        public string? ExchangeName { get; set; }
        [JsonProperty("exchange")]
        public string? Exchange { get; set; }
        [JsonProperty("regularMarketPrice")]
        public double? RegularMarketPrice { get; set; }
        [JsonProperty("previousClose")]
        public double? PreviousClose { get; set; }
        [JsonProperty("chartPreviousClose")]
        public double? ChartPreviousClose { get; set; }
        [JsonProperty("regularMarketTime")]
        public long? RegularMarketTime { get; set; }
        [JsonProperty("fiftyTwoWeekHigh")]
        public double? FiftyTwoWeekHigh { get; set; }
        [JsonProperty("fiftyTwoWeekLow")]
        public double? FiftyTwoWeekLow { get; set; }
    }
    private class YahooIndicators
    {
        [JsonProperty("quote")]
        public List<YahooOHLC>? Quote { get; set; }
    }
    private class YahooOHLC
    {
        [JsonProperty("open")]
        public List<double?>? Open { get; set; }
        [JsonProperty("high")]
        public List<double?>? High { get; set; }
        [JsonProperty("low")]
        public List<double?>? Low { get; set; }
        [JsonProperty("close")]
        public List<double?>? Close { get; set; }
        [JsonProperty("volume")]
        public List<long?>? Volume { get; set; }
    }
    private class YahooSummaryResponse
    {
        [JsonProperty("quoteSummary")]
        public YahooQuoteSummary? QuoteSummary { get; set; }
    }
    private class YahooQuoteSummary
    {
        [JsonProperty("result")]
        public List<YahooSummaryResult>? Result { get; set; }
        [JsonProperty("error")]
        public object? Error { get; set; }
    }
    private class YahooSummaryResult
    {
        [JsonProperty("price")]
        public YahooPriceData? Price { get; set; }
        [JsonProperty("summaryDetail")]
        public YahooSummaryDetail? SummaryDetail { get; set; }
        [JsonProperty("defaultKeyStatistics")]
        public YahooKeyStatistics? DefaultKeyStatistics { get; set; }
    }
    private class YahooPriceData
    {
        [JsonProperty("shortName")]
        public string? ShortName { get; set; }
        [JsonProperty("longName")]
        public string? LongName { get; set; }
        [JsonProperty("exchange")]
        public string? Exchange { get; set; }
        [JsonProperty("exchangeName")]
        public string? ExchangeName { get; set; }
        [JsonProperty("currency")]
        public string? Currency { get; set; }
        [JsonProperty("regularMarketPrice")]
        public YahooRawValue? RegularMarketPrice { get; set; }
        [JsonProperty("marketCap")]
        public YahooRawValue? MarketCap { get; set; }
    }
    private class YahooSummaryDetail
    {
        [JsonProperty("previousClose")]
        public YahooRawValue? PreviousClose { get; set; }
        [JsonProperty("dayHigh")]
        public YahooRawValue? DayHigh { get; set; }
        [JsonProperty("dayLow")]
        public YahooRawValue? DayLow { get; set; }
        [JsonProperty("volume")]
        public YahooRawValue? Volume { get; set; }
        [JsonProperty("fiftyTwoWeekHigh")]
        public YahooRawValue? FiftyTwoWeekHigh { get; set; }
        [JsonProperty("fiftyTwoWeekLow")]
        public YahooRawValue? FiftyTwoWeekLow { get; set; }
        [JsonProperty("trailingPE")]
        public YahooRawValue? TrailingPE { get; set; }
        [JsonProperty("marketCap")]
        public YahooRawValue? MarketCap { get; set; }
    }
    private class YahooKeyStatistics
    {
        [JsonProperty("trailingPE")]
        public YahooRawValue? TrailingPE { get; set; }
        [JsonProperty("forwardPE")]
        public YahooRawValue? ForwardPE { get; set; }
        [JsonProperty("enterpriseValue")]
        public YahooRawValue? EnterpriseValue { get; set; }
    }
    private class YahooRawValue
    {
        [JsonProperty("raw")]
        public double? Raw { get; set; }
        [JsonProperty("fmt")]
        public string? Fmt { get; set; }
    }
}