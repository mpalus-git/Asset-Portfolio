using PortfelStudenta.Models;
using System.Collections.Concurrent;
namespace PortfelStudenta.Services;
public interface IPortfolioService
{
    Task<PortfolioSummary> GetPortfolioSummaryAsync();
    Task<List<PortfolioPosition>> GetPositionsAsync();
    Task<List<PortfolioPosition>> GetAllPositionsAsync();
    Task<PortfolioPosition?> GetPositionAsync(string symbol);
    Task<decimal> CalculateAveragePriceAsync(string symbol);
    Task<List<PortfolioPosition>> GetTopGainersAsync(int count = 3);
    Task<List<PortfolioPosition>> GetTopLosersAsync(int count = 3);
    Task<List<(DateTime Date, decimal Value)>> GetPortfolioHistoryAsync(int days);
    void InvalidateCache();
}
public class PortfolioService : IPortfolioService
{
    private readonly IDatabaseService _databaseService;
    private readonly INbpApiService _nbpApiService;
    private readonly ICoinCapApiService _coinCapApiService;
    private readonly IYahooFinanceApiService _yahooFinanceApiService;
    private readonly ICacheService _cacheService;
    private const string PositionsCacheKey = "portfolio_positions";
    private static readonly TimeSpan PositionsCacheExpiration = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim _apiThrottle = new(3, 3);
    public PortfolioService(
        IDatabaseService databaseService,
        INbpApiService nbpApiService,
        ICoinCapApiService coinCapApiService,
        IYahooFinanceApiService yahooFinanceApiService,
        ICacheService cacheService)
    {
        _databaseService = databaseService;
        _nbpApiService = nbpApiService;
        _coinCapApiService = coinCapApiService;
        _yahooFinanceApiService = yahooFinanceApiService;
        _cacheService = cacheService;
    }
    public async Task<PortfolioSummary> GetPortfolioSummaryAsync()
    {
        var positions = await GetPositionsAsync();
        var summary = new PortfolioSummary
        {
            Positions = positions,
            TotalValue = positions.Sum(p => p.CurrentValue),
            TotalCost = positions.Sum(p => p.TotalCost),
            TotalCommission = positions.Sum(p => p.TotalCommission),
            CryptoValue = positions.Where(p => p.AssetType == AssetType.CRYPTO).Sum(p => p.CurrentValue),
            StockValue = positions.Where(p => p.AssetType == AssetType.STOCK).Sum(p => p.CurrentValue),
            CurrencyValue = positions.Where(p => p.AssetType == AssetType.CURRENCY).Sum(p => p.CurrentValue)
        };
        summary.DailyPnL = positions.Sum(p =>
        {
            if (p.ChangePercent24h.HasValue && p.ChangePercent24h != 0)
            {
                var previousValue = p.CurrentValue / (1 + p.ChangePercent24h.Value / 100);
                return p.CurrentValue - previousValue;
            }
            return 0;
        });
        return summary;
    }
    public async Task<List<PortfolioPosition>> GetPositionsAsync()
    {
        var allPositions = await GetAllPositionsAsync();
        return allPositions.Take(10).ToList();
    }
    public async Task<List<PortfolioPosition>> GetAllPositionsAsync()
    {
        var cachedPositions = _cacheService.Get<List<PortfolioPosition>>(PositionsCacheKey);
        if (cachedPositions != null)
            return cachedPositions;
        var transactions = await _databaseService.GetTransactionsAsync();
        if (!transactions.Any())
            return new List<PortfolioPosition>();
        var currencyRates = await PreloadCurrencyRatesAsync();
        var groupedTransactions = transactions
            .GroupBy(t => new { t.Symbol, t.AssetType })
            .ToList();
        var positionsWithoutPrices = new List<(PortfolioPosition Position, AssetType AssetType, string Symbol, List<Transaction> Buys)>();
        foreach (var group in groupedTransactions)
        {
            var buys = group.Where(t => t.TransactionType == TransactionType.BUY).ToList();
            var sells = group.Where(t => t.TransactionType == TransactionType.SELL).ToList();
            var buyQuantity = buys.Sum(t => t.Quantity);
            var sellQuantity = sells.Sum(t => t.Quantity);
            var netQuantity = buyQuantity - sellQuantity;
            if (netQuantity <= 0)
                continue;
            var averagePrice = CalculateWeightedAveragePrice(buys);
            var totalCommission = group.Sum(t => t.Commission);
            var lastTransactionDate = group.Max(t => t.Date);
            var position = new PortfolioPosition
            {
                AssetType = group.Key.AssetType,
                Symbol = group.Key.Symbol,
                Name = group.First().Name,
                Quantity = netQuantity,
                AveragePrice = averagePrice,
                CurrentPrice = averagePrice,
                TotalCommission = totalCommission,
                TransactionCount = group.Count(),
                LastTransactionDate = lastTransactionDate
            };
            positionsWithoutPrices.Add((position, group.Key.AssetType, group.Key.Symbol, buys));
        }
        var priceResults = new ConcurrentDictionary<string, AssetPrice?>();
        var priceTasks = positionsWithoutPrices.Select(async item =>
        {
            await _apiThrottle.WaitAsync();
            try
            {
                var price = await GetCurrentPriceWithCachedRatesAsync(
                    item.Symbol,
                    item.AssetType,
                    currencyRates);
                priceResults[item.Symbol] = price;
            }
            finally
            {
                _apiThrottle.Release();
            }
        });
        await Task.WhenAll(priceTasks);
        var positions = new List<PortfolioPosition>();
        foreach (var item in positionsWithoutPrices)
        {
            if (priceResults.TryGetValue(item.Symbol, out var currentPrice) && currentPrice != null)
            {
                item.Position.CurrentPrice = currentPrice.PricePln;
                item.Position.ChangePercent24h = currentPrice.ChangePercent24h;
            }
            positions.Add(item.Position);
        }
        var result = positions
            .OrderByDescending(p => p.LastTransactionDate)
            .ToList();
        _cacheService.Set(PositionsCacheKey, result, PositionsCacheExpiration);
        return result;
    }
    private async Task<Dictionary<string, decimal>> PreloadCurrencyRatesAsync()
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["PLN"] = 1m
        };
        var rateTasks = new []
        {
            ("USD", _nbpApiService.GetUsdPlnRateAsync()),
            ("EUR", _nbpApiService.GetEurPlnRateAsync()),
            ("GBP", _nbpApiService.GetGbpPlnRateAsync())
        };
        foreach (var (currency, task) in rateTasks)
        {
            try
            {
                rates[currency] = await task;
            }
            catch
            {
                rates[currency] = GetFallbackRate(currency);
            }
        }
        return rates;
    }
    private static decimal GetFallbackRate(string currency) => currency.ToUpperInvariant() switch
    {
        "USD" => 4.0m,
        "EUR" => 4.3m,
        "GBP" => 5.0m,
        _ => 1m
    };
    public async Task<PortfolioPosition?> GetPositionAsync(string symbol)
    {
        var positions = await GetPositionsAsync();
        return positions.FirstOrDefault(p => p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }
    public async Task<decimal> CalculateAveragePriceAsync(string symbol)
    {
        var transactions = await _databaseService.GetTransactionsBySymbolAsync(symbol);
        var buys = transactions.Where(t => t.TransactionType == TransactionType.BUY).ToList();
        return CalculateWeightedAveragePrice(buys);
    }
    public async Task<List<PortfolioPosition>> GetTopGainersAsync(int count = 3)
    {
        var positions = await GetPositionsAsync();
        return positions
            .Where(p => p.NetPnL > 0)
            .OrderByDescending(p => p.PnLPercent)
            .Take(count)
            .ToList();
    }
    public async Task<List<PortfolioPosition>> GetTopLosersAsync(int count = 3)
    {
        var positions = await GetPositionsAsync();
        return positions
            .Where(p => p.NetPnL < 0)
            .OrderBy(p => p.PnLPercent)
            .Take(count)
            .ToList();
    }
    public async Task<List<(DateTime Date, decimal Value)>> GetPortfolioHistoryAsync(int days)
    {
        var rawHistory = new List<(DateTime Date, decimal Value)>();
        var allTransactions = await _databaseService.GetTransactionsAsync();
        if (!allTransactions.Any())
            return rawHistory;
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);
        var today = DateTime.UtcNow.Date;
        var currentSummary = await GetPortfolioSummaryAsync();
        var currentTotalValue = currentSummary.TotalValue;
        var symbols = allTransactions
            .Select(t => t.Symbol)
            .Distinct()
            .ToList();
        var allPriceHistory = new Dictionary<string, List<PriceHistory>>();
        var latestPrices = new Dictionary<string, decimal>();
        foreach (var symbol in symbols)
        {
            var priceHistory = await _databaseService.GetPriceHistoryAsync(symbol, startDate, endDate.AddDays(1));
            allPriceHistory[symbol] = priceHistory;
            var latestPrice = await _databaseService.GetLatestPriceAsync(symbol);
            if (latestPrice != null)
            {
                latestPrices[symbol] = latestPrice.PricePln;
            }
            else
            {
                var avgPrice = allTransactions
                    .Where(t => t.Symbol == symbol && t.TransactionType == TransactionType.BUY)
                    .Select(t => t.UnitPrice)
                    .DefaultIfEmpty(0)
                    .Average();
                latestPrices[symbol] = avgPrice;
            }
        }
        var transactionDates = allTransactions
            .Where(t => t.Date.Date >= startDate.Date && t.Date.Date <= endDate.Date)
            .Select(t => t.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        if (!transactionDates.Contains(today))
            transactionDates.Add(today);
        if (transactionDates.Count == 0)
        {
            transactionDates.Add(startDate.Date);
            transactionDates.Add(today);
        }
        var groupedBySymbol = allTransactions
            .GroupBy(t => new { t.Symbol, t.AssetType })
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var date in transactionDates.OrderBy(d => d))
        {
            if (date == today)
            {
                rawHistory.Add((today, currentTotalValue));
                continue;
            }
            decimal dailyValue = 0;
            foreach (var kvp in groupedBySymbol)
            {
                var transactionsToDate = kvp.Value
                    .Where(t => t.Date.Date <= date)
                    .ToList();
                if (!transactionsToDate.Any())
                    continue;
                var buyQuantity = transactionsToDate
                    .Where(t => t.TransactionType == TransactionType.BUY)
                    .Sum(t => t.Quantity);
                var sellQuantity = transactionsToDate
                    .Where(t => t.TransactionType == TransactionType.SELL)
                    .Sum(t => t.Quantity);
                var netQuantity = buyQuantity - sellQuantity;
                if (netQuantity <= 0)
                    continue;
                decimal price;
                if (allPriceHistory.TryGetValue(kvp.Key.Symbol, out var symbolHistory))
                {
                    var priceOnDate = symbolHistory
                        .Where(p => p.Timestamp.Date <= date)
                        .OrderByDescending(p => p.Timestamp)
                        .FirstOrDefault();
                    price = priceOnDate?.PricePln ?? latestPrices.GetValueOrDefault(kvp.Key.Symbol, 0);
                }
                else
                {
                    price = latestPrices.GetValueOrDefault(kvp.Key.Symbol, 0);
                }
                dailyValue += netQuantity * price;
            }
            if (dailyValue > 0)
            {
                rawHistory.Add((date.Date, dailyValue));
            }
        }
        var history = rawHistory
            .GroupBy(h => h.Date.Date)
            .Select(g => (Date: g.Key, Value: g.Max(x => x.Value)))
            .OrderBy(h => h.Date)
            .ToList();
        return history;
    }
    public void InvalidateCache()
    {
        _cacheService.Remove(PositionsCacheKey);
    }
    private static decimal CalculateWeightedAveragePrice(List<Transaction> buys)
    {
        if (!buys.Any())
            return 0;
        var totalQuantity = buys.Sum(t => t.Quantity);
        if (totalQuantity == 0)
            return 0;
        var weightedSum = buys.Sum(t => t.Quantity * t.UnitPrice);
        return weightedSum / totalQuantity;
    }
    private async Task<AssetPrice?> GetCurrentPriceWithCachedRatesAsync(
        string symbol,
        AssetType assetType,
        Dictionary<string, decimal> currencyRates)
    {
        try
        {
            return assetType switch
            {
                AssetType.CURRENCY => await _nbpApiService.GetCurrencyRateAsync(symbol),
                AssetType.CRYPTO => await GetCryptoPriceBySymbolAsync(symbol),
                AssetType.STOCK => await _yahooFinanceApiService.GetStockPriceAsync(symbol),
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }
    private async Task<AssetPrice?> GetCryptoPriceBySymbolAsync(string symbol)
    {
        var symbolToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "BNB", "binance-coin" },
            { "XRP", "ripple" },
            { "ADA", "cardano" },
            { "SOL", "solana" },
            { "DOT", "polkadot" },
            { "DOGE", "dogecoin" }
        };
        if (symbolToId.TryGetValue(symbol, out var id))
        {
            return await _coinCapApiService.GetCryptoAssetAsync(id);
        }
        return await _coinCapApiService.GetCryptoAssetAsync(symbol.ToLower());
    }
}