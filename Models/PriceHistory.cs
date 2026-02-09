using SQLite;
namespace PortfelStudenta.Models;
[Table("PriceHistory")]
public class PriceHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public AssetType AssetType { get; set; }
    [Indexed]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
    public decimal PricePln { get; set; }
    public decimal? PriceOriginal { get; set; }
    [MaxLength(10)]
    public string? OriginalCurrency { get; set; }
    [Indexed]
    public DateTime Timestamp { get; set; }
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;
}