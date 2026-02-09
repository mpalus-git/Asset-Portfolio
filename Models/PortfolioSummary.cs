namespace PortfelStudenta.Models;
public class PortfolioSummary
{
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalNetPnL => TotalValue - TotalCost - TotalCommission;
    public decimal TotalPnLPercent => TotalCost > 0 ? (TotalNetPnL / TotalCost) * 100 : 0;
    public decimal DailyPnL { get; set; }
    public decimal CryptoValue { get; set; }
    public decimal StockValue { get; set; }
    public decimal CurrencyValue { get; set; }
    public decimal CryptoPercent => TotalValue > 0 ? (CryptoValue / TotalValue) * 100 : 0;
    public decimal StockPercent => TotalValue > 0 ? (StockValue / TotalValue) * 100 : 0;
    public decimal CurrencyPercent => TotalValue > 0 ? (CurrencyValue / TotalValue) * 100 : 0;
    public List<PortfolioPosition> Positions { get; set; } = new();
    public bool IsProfitable => TotalNetPnL > 0;
}