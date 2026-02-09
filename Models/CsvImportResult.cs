namespace PortfelStudenta.Models;
public class CsvImportResult
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => SuccessCount > 0;
    public int TotalRows => SuccessCount + ErrorCount;
}