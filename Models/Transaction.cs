using SQLite;
namespace PortfelStudenta.Models;
[Table("Transactions")]
public class Transaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public AssetType AssetType { get; set; }
    [Indexed]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Commission { get; set; }
    [MaxLength(10)]
    public string CommissionCurrency { get; set; } = "PLN";
    [Indexed]
    public DateTime Date { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}