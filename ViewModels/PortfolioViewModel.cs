using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using PortfelStudenta.Models;
using PortfelStudenta.Services;
namespace PortfelStudenta.ViewModels;
public class PortfolioViewModel : BaseViewModel
{
    private readonly IPortfolioService _portfolioService;
    private readonly IDatabaseService _databaseService;
    private PortfolioSummary? _summary;
    private PortfolioPosition? _selectedPosition;
    private bool _hasPositions;
    private bool _hasMorePositions;
    private bool _showingAllPositions;
    private bool _showBuyTransactions = true;
    private string _transactionTypeLabel = "Kupno";
    public PortfolioSummary? Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }
    public PortfolioPosition? SelectedPosition
    {
        get => _selectedPosition;
        set => SetProperty(ref _selectedPosition, value);
    }
    public bool HasPositions
    {
        get => _hasPositions;
        set => SetProperty(ref _hasPositions, value);
    }
    public bool HasMorePositions
    {
        get => _hasMorePositions;
        set => SetProperty(ref _hasMorePositions, value);
    }
    public bool ShowingAllPositions
    {
        get => _showingAllPositions;
        set => SetProperty(ref _showingAllPositions, value);
    }
    public bool ShowBuyTransactions
    {
        get => _showBuyTransactions;
        set => SetProperty(ref _showBuyTransactions, value);
    }
    public string TransactionTypeLabel
    {
        get => _transactionTypeLabel;
        set => SetProperty(ref _transactionTypeLabel, value);
    }
    public ObservableCollection<PortfolioPosition> Positions { get; } = new();
    public ObservableCollection<PortfolioPosition> RemainingPositions { get; } = new();
    public ObservableCollection<Transaction> RecentTransactions { get; } = new();
    public ObservableCollection<Transaction> FilteredTransactions { get; } = new();
    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddTransactionCommand { get; }
    public IAsyncRelayCommand<PortfolioPosition> ViewPositionDetailsCommand { get; }
    public IAsyncRelayCommand<Transaction> DeleteTransactionCommand { get; }
    public IRelayCommand ShowAllPositionsCommand { get; }
    public IAsyncRelayCommand ToggleTransactionTypeCommand { get; }
    public PortfolioViewModel(IPortfolioService portfolioService, IDatabaseService databaseService)
    {
        _portfolioService = portfolioService;
        _databaseService = databaseService;
        Title = "Mój Portfel";
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        AddTransactionCommand = new AsyncRelayCommand(AddTransactionAsync);
        ViewPositionDetailsCommand = new AsyncRelayCommand<PortfolioPosition>(ViewPositionDetailsAsync);
        DeleteTransactionCommand = new AsyncRelayCommand<Transaction>(DeleteTransactionAsync);
        ShowAllPositionsCommand = new RelayCommand(ShowAllPositions);
        ToggleTransactionTypeCommand = new AsyncRelayCommand(ToggleTransactionTypeAsync);
    }
    private async Task ToggleTransactionTypeAsync()
    {
        ShowBuyTransactions = !ShowBuyTransactions;
        TransactionTypeLabel = ShowBuyTransactions ? "Kupno" : "Sprzedaż";
        await LoadFilteredTransactionsAsync();
    }
    private async Task LoadFilteredTransactionsAsync()
    {
        var transactionType = ShowBuyTransactions ? TransactionType.BUY : TransactionType.SELL;
        var transactions = await _databaseService.GetTransactionsByTransactionTypeAsync(transactionType);
        UpdateCollection(FilteredTransactions, transactions);
    }
    private async Task LoadDataAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var allPositions = await _portfolioService.GetAllPositionsAsync();
            var top10 = allPositions.Take(10).ToList();
            var remaining = allPositions.Skip(10).ToList();
            Summary = await _portfolioService.GetPortfolioSummaryAsync();
            UpdateCollection(Positions, top10);
            UpdateCollection(RemainingPositions, remaining);
            HasPositions = Positions.Count > 0;
            HasMorePositions = remaining.Count > 0;
            ShowingAllPositions = false;
            var transactions = await _databaseService.GetRecentTransactionsAsync(10);
            UpdateCollection(RecentTransactions, transactions);
            await LoadFilteredTransactionsAsync();
        });
    }
    private void ShowAllPositions()
    {
        ShowingAllPositions = true;
    }
    private static void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T> newItems)
    {
        var itemsList = newItems.ToList();
        if (collection.Count == 0 || Math.Abs(collection.Count - itemsList.Count) > 2)
        {
            collection.Clear();
            foreach (var item in itemsList)
                collection.Add(item);
            return;
        }
        while (collection.Count > itemsList.Count)
            collection.RemoveAt(collection.Count - 1);
        for (int i = 0; i < itemsList.Count; i++)
        {
            if (i < collection.Count)
                collection[i] = itemsList[i];
            else
                collection.Add(itemsList[i]);
        }
    }
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        _portfolioService.InvalidateCache();
        await LoadDataAsync();
    }
    private async Task AddTransactionAsync()
    {
        await Shell.Current.GoToAsync("AddTransactionPage");
    }
    private async Task ViewPositionDetailsAsync(PortfolioPosition? position)
    {
        if (position == null)
            return;
        SelectedPosition = position;
        var message = $"Symbol: {position.Symbol}\n" +
                      $"Ilość: {position.Quantity:N4}\n" +
                      $"Średnia cena: {position.AveragePrice:N2} PLN\n" +
                      $"Aktualna cena: {position.CurrentPrice:N2} PLN\n" +
                      $"Wartość: {position.CurrentValue:N2} PLN\n" +
                      $"P&L netto: {position.NetPnL:N2} PLN ({position.PnLPercent:N2}%)\n" +
                      $"Prowizje: {position.TotalCommission:N2} PLN\n" +
                      $"Liczba transakcji: {position.TransactionCount}";
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
        {
            await page.DisplayAlertAsync($"Pozycja: {position.Name}", message, "OK");
        }
    }
    private async Task DeleteTransactionAsync(Transaction? transaction)
    {
        if (transaction == null)
            return;
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
        {
            var confirm = await page.DisplayAlertAsync(
                "Usuń transakcję",
                $"Czy na pewno chcesz usunąć transakcję {transaction.TransactionType} {transaction.Quantity} {transaction.Symbol}?",
                "Usuń", "Anuluj");
            if (confirm)
            {
                await _databaseService.DeleteTransactionAsync(transaction);
                _portfolioService.InvalidateCache();
                await LoadDataAsync();
            }
        }
    }
}