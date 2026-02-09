namespace PortfelStudenta.Models;
public class AssetPrice
{
    public AssetType AssetType { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal PricePln { get; set; }
    public decimal? PriceOriginal { get; set; }
    public string? OriginalCurrency { get; set; }
    public decimal? ChangePercent24h { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsFromCache { get; set; }
    public decimal? HighPrice24h { get; set; }
    public decimal? LowPrice24h { get; set; }
    public decimal? PriceChange24h { get; set; }
    public decimal? Volume24h { get; set; }
    public decimal? QuoteVolume24h { get; set; }
    public decimal? OpenPrice { get; set; }
    public long? TradeCount { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? FiftyTwoWeekHigh { get; set; }
    public decimal? FiftyTwoWeekLow { get; set; }
    public string? Exchange { get; set; }
    public List<CryptoKline>? Klines { get; set; }
}
public class CryptoKline
{
    public DateTime OpenTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime CloseTime { get; set; }
}