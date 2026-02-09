using System.Globalization;
using System.Text;
using PortfelStudenta.Models;
namespace PortfelStudenta.Services;
public interface ICsvService
{
    Task<string> ExportTransactionsAsync();
    Task<CsvImportResult> ImportTransactionsAsync(string csvContent);
    Task<string> GetExportFilePathAsync();
}
public class CsvService : ICsvService
{
    private readonly IDatabaseService _databaseService;
    private const string CsvHeader = "AssetType,Symbol,Name,TransactionType,Quantity,UnitPrice,TotalValue,Commission,CommissionCurrency,Date,Notes";
    private const int MaxCsvSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxCsvLines = 50_000;
    public CsvService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }
    public async Task<string> ExportTransactionsAsync()
    {
        var transactions = await _databaseService.GetTransactionsAsync();
        var sb = new StringBuilder();
        sb.AppendLine(CsvHeader);
        foreach (var t in transactions)
        {
            var row = string.Join(",",
                EscapeCsvField(t.AssetType.ToString()),
                EscapeCsvField(t.Symbol),
                EscapeCsvField(t.Name),
                EscapeCsvField(t.TransactionType.ToString()),
                t.Quantity.ToString(CultureInfo.InvariantCulture),
                t.UnitPrice.ToString(CultureInfo.InvariantCulture),
                t.TotalValue.ToString(CultureInfo.InvariantCulture),
                t.Commission.ToString(CultureInfo.InvariantCulture),
                EscapeCsvField(t.CommissionCurrency),
                t.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                EscapeCsvField(t.Notes ?? "")
            );
            sb.AppendLine(row);
        }
        return sb.ToString();
    }
    public async Task<CsvImportResult> ImportTransactionsAsync(string csvContent)
    {
        var result = new CsvImportResult();
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            result.Errors.Add("Plik CSV jest pusty.");
            return result;
        }
        if (csvContent.Length > MaxCsvSizeBytes)
        {
            result.Errors.Add($"Plik CSV jest za duży (maks. {MaxCsvSizeBytes / 1024 / 1024} MB).");
            return result;
        }
        var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            result.Errors.Add("Plik CSV musi zawierać nagłówek i przynajmniej jeden wiersz danych.");
            return result;
        }
        if (lines.Length > MaxCsvLines)
        {
            result.Errors.Add($"Plik CSV zawiera za dużo wierszy (maks. {MaxCsvLines}).");
            return result;
        }
        var header = lines[0].Trim();
        if (!ValidateHeader(header))
        {
            result.Errors.Add($"Nieprawidłowy nagłówek CSV. Oczekiwany: {CsvHeader}");
            return result;
        }
        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var transaction = ParseCsvLine(line, lineNumber);
                await _databaseService.SaveTransactionAsync(transaction);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Wiersz {lineNumber}: {ex.Message}");
            }
        }
        return result;
    }
    public async Task<string> GetExportFilePathAsync()
    {
        var fileName = $"PortfelStudenta_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        var csvContent = await ExportTransactionsAsync();
        await File.WriteAllTextAsync(path, csvContent, Encoding.UTF8);
        return path;
    }
    private bool ValidateHeader(string header)
    {
        var expectedFields = CsvHeader.Split(',');
        var actualFields = ParseCsvLine(header).Select(f => f.Trim()).ToArray();
        if (actualFields.Length != expectedFields.Length)
            return false;
        for (int i = 0; i < expectedFields.Length; i++)
        {
            if (!string.Equals(expectedFields[i], actualFields[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
    private Transaction ParseCsvLine(string line, int lineNumber)
    {
        var fields = ParseCsvLine(line);
        if (fields.Length < 10)
        {
            throw new FormatException($"Za mało pól (oczekiwano min. 10, otrzymano {fields.Length})");
        }
        if (!Enum.TryParse<AssetType>(fields[0].Trim(), true, out var assetType))
        {
            throw new FormatException($"Nieprawidłowy typ aktywa: {fields[0]}. Dozwolone: CRYPTO, STOCK, CURRENCY");
        }
        if (!Enum.TryParse<TransactionType>(fields[3].Trim(), true, out var transactionType))
        {
            throw new FormatException($"Nieprawidłowy typ transakcji: {fields[3]}. Dozwolone: BUY, SELL");
        }
        if (!decimal.TryParse(fields[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
        {
            throw new FormatException($"Nieprawidłowa ilość: {fields[4]}. Musi być liczbą większą od 0.");
        }
        if (!decimal.TryParse(fields[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice <= 0)
        {
            throw new FormatException($"Nieprawidłowa cena jednostkowa: {fields[5]}. Musi być liczbą większą od 0.");
        }
        decimal.TryParse(fields[6].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var totalValue);
        if (!decimal.TryParse(fields[7].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var commission))
        {
            commission = 0;
        }
        if (!DateTime.TryParse(fields[9].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            if (!DateTime.TryParse(fields[9].Trim(), out date))
            {
                throw new FormatException($"Nieprawidłowa data: {fields[9]}");
            }
        }
        return new Transaction
        {
            AssetType = assetType,
            Symbol = fields[1].Trim().ToUpper(),
            Name = fields[2].Trim(),
            TransactionType = transactionType,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalValue = totalValue > 0 ? totalValue : quantity * unitPrice,
            Commission = commission,
            CommissionCurrency = string.IsNullOrWhiteSpace(fields[8]) ? "PLN" : fields[8].Trim().ToUpper(),
            Date = date,
            Notes = fields.Length > 10 ? fields[10].Trim() : null
        };
    }
    private string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }
        fields.Add(currentField.ToString());
        return fields.ToArray();
    }
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}