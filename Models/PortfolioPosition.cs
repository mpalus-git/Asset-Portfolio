namespace PortfelStudenta.Models;
public class PortfolioPosition
{
    public AssetType AssetType { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue => Quantity * CurrentPrice;
    public decimal TotalCost => Quantity * AveragePrice;
    public decimal TotalCommission { get; set; }
    public decimal GrossPnL => CurrentValue - TotalCost;
    public decimal NetPnL => GrossPnL - TotalCommission;
    public decimal PnLPercent => TotalCost > 0 ? (NetPnL / TotalCost) * 100 : 0;
    public decimal? ChangePercent24h { get; set; }
    public int TransactionCount { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public bool IsProfitable => NetPnL > 0;
}