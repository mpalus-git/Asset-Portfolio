using SQLite;
using PortfelStudenta.Models;
namespace PortfelStudenta.Services;
public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<Transaction>> GetTransactionsAsync();
    Task<List<Transaction>> GetRecentTransactionsAsync(int limit = 10);
    Task<List<Transaction>> GetTransactionsBySymbolAsync(string symbol);
    Task<List<Transaction>> GetTransactionsByAssetTypeAsync(AssetType assetType);
    Task<List<Transaction>> GetTransactionsByTransactionTypeAsync(TransactionType transactionType);
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<int> SaveTransactionAsync(Transaction transaction);
    Task<int> DeleteTransactionAsync(Transaction transaction);
    Task<int> DeleteAllTransactionsAsync();
    Task<List<PriceHistory>> GetPriceHistoryAsync(string symbol, DateTime from, DateTime to);
    Task<Dictionary<string, List<PriceHistory>>> GetBatchPriceHistoryAsync(IEnumerable<string> symbols, DateTime from, DateTime to);
    Task<PriceHistory?> GetLatestPriceAsync(string symbol);
    Task<Dictionary<string, PriceHistory>> GetBatchLatestPricesAsync(IEnumerable<string> symbols);
    Task<int> SavePriceHistoryAsync(PriceHistory priceHistory);
    Task<int> DeleteOldPriceHistoryAsync(DateTime olderThan);
    Task<int> GetTransactionCountAsync();
}
public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "portfelstudenta.db3");
    }
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;
        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return;
            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _database.CreateTableAsync<Transaction>();
            await _database.CreateTableAsync<PriceHistory>();
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
            await InitializeAsync();
    }
    #region Transactions
    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>().OrderByDescending(t => t.Date).ToListAsync();
    }
    public async Task<List<Transaction>> GetRecentTransactionsAsync(int limit = 10)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>()
            .OrderByDescending(t => t.Date)
            .Take(limit)
            .ToListAsync();
    }
    public async Task<List<Transaction>> GetTransactionsBySymbolAsync(string symbol)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>()
            .Where(t => t.Symbol == symbol)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    public async Task<List<Transaction>> GetTransactionsByAssetTypeAsync(AssetType assetType)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>()
            .Where(t => t.AssetType == assetType)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    public async Task<List<Transaction>> GetTransactionsByTransactionTypeAsync(TransactionType transactionType)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>()
            .Where(t => t.TransactionType == transactionType)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>().FirstOrDefaultAsync(t => t.Id == id);
    }
    public async Task<int> SaveTransactionAsync(Transaction transaction)
    {
        await EnsureInitializedAsync();
        if (transaction.Id != 0)
            return await _database!.UpdateAsync(transaction);
        transaction.CreatedAt = DateTime.UtcNow;
        return await _database!.InsertAsync(transaction);
    }
    public async Task<int> DeleteTransactionAsync(Transaction transaction)
    {
        await EnsureInitializedAsync();
        return await _database!.DeleteAsync(transaction);
    }
    public async Task<int> DeleteAllTransactionsAsync()
    {
        await EnsureInitializedAsync();
        return await _database!.DeleteAllAsync<Transaction>();
    }
    #endregion
    #region PriceHistory
    public async Task<List<PriceHistory>> GetPriceHistoryAsync(string symbol, DateTime from, DateTime to)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<PriceHistory>()
            .Where(p => p.Symbol == symbol && p.Timestamp >= from && p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
    }
    public async Task<Dictionary<string, List<PriceHistory>>> GetBatchPriceHistoryAsync(
        IEnumerable<string> symbols,
        DateTime from,
        DateTime to)
    {
        await EnsureInitializedAsync();
        var symbolList = symbols.ToList();
        if (!symbolList.Any())
            return new Dictionary<string, List<PriceHistory>>();
        var allHistory = await _database!.Table<PriceHistory>()
            .Where(p => p.Timestamp >= from && p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
        return allHistory
            .Where(p => symbolList.Contains(p.Symbol))
            .GroupBy(p => p.Symbol)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
    public async Task<PriceHistory?> GetLatestPriceAsync(string symbol)
    {
        await EnsureInitializedAsync();
        return await _database!.Table<PriceHistory>()
            .Where(p => p.Symbol == symbol)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync();
    }
    public async Task<Dictionary<string, PriceHistory>> GetBatchLatestPricesAsync(IEnumerable<string> symbols)
    {
        await EnsureInitializedAsync();
        var symbolList = symbols.ToList();
        if (!symbolList.Any())
            return new Dictionary<string, PriceHistory>();
        var result = new Dictionary<string, PriceHistory>();
        var recentDate = DateTime.UtcNow.AddDays(-7);
        var allRecent = await _database!.Table<PriceHistory>()
            .Where(p => p.Timestamp >= recentDate)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync();
        foreach (var symbol in symbolList)
        {
            var latest = allRecent.FirstOrDefault(p => p.Symbol == symbol);
            if (latest != null)
            {
                result[symbol] = latest;
            }
        }
        return result;
    }
    public async Task<int> SavePriceHistoryAsync(PriceHistory priceHistory)
    {
        await EnsureInitializedAsync();
        var existing = await _database!.Table<PriceHistory>()
            .Where(p => p.Symbol == priceHistory.Symbol &&
                        p.Timestamp > priceHistory.Timestamp.AddMinutes(-1) &&
                        p.Timestamp < priceHistory.Timestamp.AddMinutes(1))
            .FirstOrDefaultAsync();
        if (existing != null)
            return 0;
        return await _database!.InsertAsync(priceHistory);
    }
    public async Task<int> DeleteOldPriceHistoryAsync(DateTime olderThan)
    {
        await EnsureInitializedAsync();
        return await _database!.ExecuteAsync(
            "DELETE FROM PriceHistory WHERE Timestamp < ?",
            olderThan);
    }
    #endregion
    #region Statistics
    public async Task<int> GetTransactionCountAsync()
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Transaction>().CountAsync();
    }
    #endregion
}