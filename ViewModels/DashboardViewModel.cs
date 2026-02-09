using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using SkiaSharp;
using PortfelStudenta.Models;
using PortfelStudenta.Services;
namespace PortfelStudenta.ViewModels;
public class DashboardViewModel : BaseViewModel
{
    private readonly IPortfolioService _portfolioService;
    private readonly IDatabaseService _databaseService;
    private PortfolioSummary? _portfolioSummary;
    private Chart? _portfolioValueChart;
    private Chart? _allocationChart;
    private int _selectedPeriodIndex;
    private bool _hasPositions;
    private int _transactionCount;
    public PortfolioSummary? PortfolioSummary
    {
        get => _portfolioSummary;
        set => SetProperty(ref _portfolioSummary, value);
    }
    public Chart? PortfolioValueChart
    {
        get => _portfolioValueChart;
        set => SetProperty(ref _portfolioValueChart, value);
    }
    public Chart? AllocationChart
    {
        get => _allocationChart;
        set => SetProperty(ref _allocationChart, value);
    }
    public int SelectedPeriodIndex
    {
        get => _selectedPeriodIndex;
        set => SetProperty(ref _selectedPeriodIndex, value);
    }
    public bool HasPositions
    {
        get => _hasPositions;
        set => SetProperty(ref _hasPositions, value);
    }
    public int TransactionCount
    {
        get => _transactionCount;
        set => SetProperty(ref _transactionCount, value);
    }
    public ObservableCollection<PortfolioPosition> TopGainers { get; } = new();
    public ObservableCollection<PortfolioPosition> TopLosers { get; } = new();
    private readonly int[] _periodDays = { 7, 30, 90, 365 };
    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<int> ChangePeriodCommand { get; }
    public DashboardViewModel(IPortfolioService portfolioService, IDatabaseService databaseService)
    {
        _portfolioService = portfolioService;
        _databaseService = databaseService;
        Title = "Dashboard";
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ChangePeriodCommand = new AsyncRelayCommand<int>(ChangePeriodAsync);
    }
    private async Task LoadDataAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var summaryTask = _portfolioService.GetPortfolioSummaryAsync();
            var transactionCountTask = _databaseService.GetTransactionCountAsync();
            await Task.WhenAll(summaryTask, transactionCountTask);
            PortfolioSummary = summaryTask.Result;
            TransactionCount = transactionCountTask.Result;
            HasPositions = PortfolioSummary.Positions.Count > 0;
            if (HasPositions)
            {
                await Task.WhenAll(
                    LoadChartsAsync(),
                    LoadTopPositionsAsync()
                );
            }
        });
    }
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        _portfolioService.InvalidateCache();
        await LoadDataAsync();
    }
    private async Task ChangePeriodAsync(int index)
    {
        SelectedPeriodIndex = index;
        await LoadPortfolioChartAsync();
    }
    private async Task LoadChartsAsync()
    {
        var chartTask = LoadPortfolioChartAsync();
        LoadAllocationChart();
        await chartTask;
    }
    private async Task LoadPortfolioChartAsync()
    {
        if (PortfolioSummary == null || !HasPositions)
            return;
        var days = _periodDays[SelectedPeriodIndex];
        var history = await _portfolioService.GetPortfolioHistoryAsync(days);
        if (history.Count > 0)
        {
            var uniqueHistory = history
                .GroupBy(h => h.Date.Date)
                .Select(g => (Date: g.Key, Value: g.Max(x => x.Value)))
                .OrderBy(h => h.Date)
                .ToList();
            var entries = new List<ChartEntry>(uniqueHistory.Count);
            var minValue = (float)uniqueHistory.Min(h => h.Value);
            var maxValue = (float)uniqueHistory.Max(h => h.Value);
            var range = maxValue - minValue;
            if (range == 0) range = maxValue * 0.1f;
            minValue -= range * 0.05f;
            maxValue += range * 0.05f;
            foreach (var h in uniqueHistory)
            {
                entries.Add(new ChartEntry((float)h.Value)
                {
                    Label = h.Date.ToString("dd.MM"),
                    ValueLabel = h.Value.ToString("N0"),
                    Color = SKColor.Parse("#2196F3"),
                    ValueLabelColor = SKColors.White
                });
            }
            PortfolioValueChart = new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Straight,
                LineSize = 3,
                PointMode = PointMode.Circle,
                PointSize = 10,
                LineAreaAlpha = 0,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 24,
                ValueLabelOrientation = Orientation.Horizontal,
                MinValue = minValue,
                MaxValue = maxValue,
                EnableYFadeOutGradient = false
            };
        }
    }
    private void LoadAllocationChart()
    {
        if (PortfolioSummary == null || !HasPositions)
            return;
        var entries = new List<ChartEntry>(3);
        if (PortfolioSummary.CryptoValue > 0)
        {
            entries.Add(new ChartEntry((float)PortfolioSummary.CryptoPercent)
            {
                Label = "Krypto",
                ValueLabel = $"{PortfolioSummary.CryptoPercent:N1}%",
                Color = SKColor.Parse("#FF9800"),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
            });
        }
        if (PortfolioSummary.StockValue > 0)
        {
            entries.Add(new ChartEntry((float)PortfolioSummary.StockPercent)
            {
                Label = "Akcje",
                ValueLabel = $"{PortfolioSummary.StockPercent:N1}%",
                Color = SKColor.Parse("#4CAF50"),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
            });
        }
        if (PortfolioSummary.CurrencyValue > 0)
        {
            entries.Add(new ChartEntry((float)PortfolioSummary.CurrencyPercent)
            {
                Label = "Waluty",
                ValueLabel = $"{PortfolioSummary.CurrencyPercent:N1}%",
                Color = SKColor.Parse("#2196F3"),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
            });
        }
        if (entries.Count > 0)
        {
            AllocationChart = new DonutChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 28,
                HoleRadius = 0.5f
            };
        }
    }
    private async Task LoadTopPositionsAsync()
    {
        var gainersTask = _portfolioService.GetTopGainersAsync(3);
        var losersTask = _portfolioService.GetTopLosersAsync(3);
        await Task.WhenAll(gainersTask, losersTask);
        UpdateCollection(TopGainers, gainersTask.Result);
        UpdateCollection(TopLosers, losersTask.Result);
    }
    private static void UpdateCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> newItems)
    {
        collection.Clear();
        foreach (var item in newItems)
            collection.Add(item);
    }
}