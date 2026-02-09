using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using PortfelStudenta.Models;
using PortfelStudenta.Services;
using SkiaSharp;
namespace PortfelStudenta.ViewModels;
public class MarketsViewModel : BaseViewModel
{
    private readonly INbpApiService _nbpApiService;
    private readonly ICoinCapApiService _coinCapApiService;
    private readonly IYahooFinanceApiService _yahooFinanceApiService;
    private bool _isCurrenciesLoading;
    private bool _isCryptosLoading;
    private bool _isStocksLoading;
    private string? _currenciesError;
    private string? _cryptosError;
    private string? _stocksError;
    private bool _isMoreCurrenciesExpanded;
    private bool _isMoreCurrenciesLoading;
    private AssetPrice? _selectedAdditionalCurrency;
    private string _cryptoSearchQuery = "";
    private bool _isCryptoSearching;
    private AssetPrice? _searchedCrypto;
    private string? _cryptoSearchError;
    private Chart? _searchedCryptoChart;
    private bool _isSearchedChartLoading;
    private string _searchedChartInterval = "24h";
    private string _searchedChartMinPrice = "";
    private string _searchedChartMaxPrice = "";
    private string _searchedChartMidPrice = "";
    private string _searchedChartStartDate = "";
    private string _searchedChartMidDate = "";
    private string _searchedChartEndDate = "";
    private bool _isAdvancedViewEnabled;
    private string _selectedChartInterval = "24h";
    private bool _isChartLoading;
    private Chart? _cryptoChart;
    private AssetPrice? _selectedCryptoForChart;
    private string _chartMinPrice = "";
    private string _chartMaxPrice = "";
    private string _chartMidPrice = "";
    private string _chartStartDate = "";
    private string _chartMidDate = "";
    private string _chartEndDate = "";
    private AssetPrice? _converterFromCurrency;
    private AssetPrice? _converterToCurrency;
    private string _converterFromAmount = "";
    private string _converterToAmount = "";
    private bool _isConverterFromPickerVisible;
    private bool _isConverterToPickerVisible;
    private string _stockSearchQuery = "";
    private bool _isStockSearching;
    private AssetPrice? _searchedStock;
    private string? _stockSearchError;
    private Chart? _searchedStockChart;
    private bool _isSearchedStockChartLoading;
    private string _searchedStockChartInterval = "1msc.";
    private string _searchedStockChartMinPrice = "";
    private string _searchedStockChartMaxPrice = "";
    private string _searchedStockChartMidPrice = "";
    private string _searchedStockChartStartDate = "";
    private string _searchedStockChartMidDate = "";
    private string _searchedStockChartEndDate = "";
    public bool IsCurrenciesLoading
    {
        get => _isCurrenciesLoading;
        set => SetProperty(ref _isCurrenciesLoading, value);
    }
    public bool IsCryptosLoading
    {
        get => _isCryptosLoading;
        set => SetProperty(ref _isCryptosLoading, value);
    }
    public bool IsStocksLoading
    {
        get => _isStocksLoading;
        set => SetProperty(ref _isStocksLoading, value);
    }
    public string? CurrenciesError
    {
        get => _currenciesError;
        set => SetProperty(ref _currenciesError, value);
    }
    public string? CryptosError
    {
        get => _cryptosError;
        set => SetProperty(ref _cryptosError, value);
    }
    public string? StocksError
    {
        get => _stocksError;
        set => SetProperty(ref _stocksError, value);
    }
    public bool IsMoreCurrenciesExpanded
    {
        get => _isMoreCurrenciesExpanded;
        set => SetProperty(ref _isMoreCurrenciesExpanded, value);
    }
    public bool IsMoreCurrenciesLoading
    {
        get => _isMoreCurrenciesLoading;
        set => SetProperty(ref _isMoreCurrenciesLoading, value);
    }
    public AssetPrice? SelectedAdditionalCurrency
    {
        get => _selectedAdditionalCurrency;
        set => SetProperty(ref _selectedAdditionalCurrency, value);
    }
    public string CryptoSearchQuery
    {
        get => _cryptoSearchQuery;
        set => SetProperty(ref _cryptoSearchQuery, value);
    }
    public bool IsCryptoSearching
    {
        get => _isCryptoSearching;
        set => SetProperty(ref _isCryptoSearching, value);
    }
    public AssetPrice? SearchedCrypto
    {
        get => _searchedCrypto;
        set => SetProperty(ref _searchedCrypto, value);
    }
    public string? CryptoSearchError
    {
        get => _cryptoSearchError;
        set => SetProperty(ref _cryptoSearchError, value);
    }
    public Chart? SearchedCryptoChart
    {
        get => _searchedCryptoChart;
        set => SetProperty(ref _searchedCryptoChart, value);
    }
    public bool IsSearchedChartLoading
    {
        get => _isSearchedChartLoading;
        set => SetProperty(ref _isSearchedChartLoading, value);
    }
    public string SearchedChartInterval
    {
        get => _searchedChartInterval;
        set => SetProperty(ref _searchedChartInterval, value);
    }
    public string SearchedChartMinPrice
    {
        get => _searchedChartMinPrice;
        set => SetProperty(ref _searchedChartMinPrice, value);
    }
    public string SearchedChartMaxPrice
    {
        get => _searchedChartMaxPrice;
        set => SetProperty(ref _searchedChartMaxPrice, value);
    }
    public string SearchedChartMidPrice
    {
        get => _searchedChartMidPrice;
        set => SetProperty(ref _searchedChartMidPrice, value);
    }
    public string SearchedChartStartDate
    {
        get => _searchedChartStartDate;
        set => SetProperty(ref _searchedChartStartDate, value);
    }
    public string SearchedChartMidDate
    {
        get => _searchedChartMidDate;
        set => SetProperty(ref _searchedChartMidDate, value);
    }
    public string SearchedChartEndDate
    {
        get => _searchedChartEndDate;
        set => SetProperty(ref _searchedChartEndDate, value);
    }
    public bool IsAdvancedViewEnabled
    {
        get => _isAdvancedViewEnabled;
        set => SetProperty(ref _isAdvancedViewEnabled, value);
    }
    public string SelectedChartInterval
    {
        get => _selectedChartInterval;
        set => SetProperty(ref _selectedChartInterval, value);
    }
    public bool IsChartLoading
    {
        get => _isChartLoading;
        set => SetProperty(ref _isChartLoading, value);
    }
    public Chart? CryptoChart
    {
        get => _cryptoChart;
        set => SetProperty(ref _cryptoChart, value);
    }
    public AssetPrice? SelectedCryptoForChart
    {
        get => _selectedCryptoForChart;
        set
        {
            if (SetProperty(ref _selectedCryptoForChart, value) && value != null)
            {
                _ = LoadChartDataAsync(value.Symbol, SelectedChartInterval);
            }
        }
    }
    public string ChartMinPrice
    {
        get => _chartMinPrice;
        set => SetProperty(ref _chartMinPrice, value);
    }
    public string ChartMaxPrice
    {
        get => _chartMaxPrice;
        set => SetProperty(ref _chartMaxPrice, value);
    }
    public string ChartMidPrice
    {
        get => _chartMidPrice;
        set => SetProperty(ref _chartMidPrice, value);
    }
    public string ChartStartDate
    {
        get => _chartStartDate;
        set => SetProperty(ref _chartStartDate, value);
    }
    public string ChartMidDate
    {
        get => _chartMidDate;
        set => SetProperty(ref _chartMidDate, value);
    }
    public string ChartEndDate
    {
        get => _chartEndDate;
        set => SetProperty(ref _chartEndDate, value);
    }
    public AssetPrice? ConverterFromCurrency
    {
        get => _converterFromCurrency;
        set
        {
            if (SetProperty(ref _converterFromCurrency, value))
                CalculateConversion();
        }
    }
    public AssetPrice? ConverterToCurrency
    {
        get => _converterToCurrency;
        set
        {
            if (SetProperty(ref _converterToCurrency, value))
                CalculateConversion();
        }
    }
    public string ConverterFromAmount
    {
        get => _converterFromAmount;
        set
        {
            if (SetProperty(ref _converterFromAmount, value))
                CalculateConversion();
        }
    }
    public string ConverterToAmount
    {
        get => _converterToAmount;
        set => SetProperty(ref _converterToAmount, value);
    }
    public bool IsConverterFromPickerVisible
    {
        get => _isConverterFromPickerVisible;
        set => SetProperty(ref _isConverterFromPickerVisible, value);
    }
    public bool IsConverterToPickerVisible
    {
        get => _isConverterToPickerVisible;
        set => SetProperty(ref _isConverterToPickerVisible, value);
    }
    public string StockSearchQuery
    {
        get => _stockSearchQuery;
        set => SetProperty(ref _stockSearchQuery, value);
    }
    public bool IsStockSearching
    {
        get => _isStockSearching;
        set => SetProperty(ref _isStockSearching, value);
    }
    public AssetPrice? SearchedStock
    {
        get => _searchedStock;
        set => SetProperty(ref _searchedStock, value);
    }
    public string? StockSearchError
    {
        get => _stockSearchError;
        set => SetProperty(ref _stockSearchError, value);
    }
    public Chart? SearchedStockChart
    {
        get => _searchedStockChart;
        set => SetProperty(ref _searchedStockChart, value);
    }
    public bool IsSearchedStockChartLoading
    {
        get => _isSearchedStockChartLoading;
        set => SetProperty(ref _isSearchedStockChartLoading, value);
    }
    public string SearchedStockChartInterval
    {
        get => _searchedStockChartInterval;
        set => SetProperty(ref _searchedStockChartInterval, value);
    }
    public string SearchedStockChartMinPrice
    {
        get => _searchedStockChartMinPrice;
        set => SetProperty(ref _searchedStockChartMinPrice, value);
    }
    public string SearchedStockChartMaxPrice
    {
        get => _searchedStockChartMaxPrice;
        set => SetProperty(ref _searchedStockChartMaxPrice, value);
    }
    public string SearchedStockChartMidPrice
    {
        get => _searchedStockChartMidPrice;
        set => SetProperty(ref _searchedStockChartMidPrice, value);
    }
    public string SearchedStockChartStartDate
    {
        get => _searchedStockChartStartDate;
        set => SetProperty(ref _searchedStockChartStartDate, value);
    }
    public string SearchedStockChartMidDate
    {
        get => _searchedStockChartMidDate;
        set => SetProperty(ref _searchedStockChartMidDate, value);
    }
    public string SearchedStockChartEndDate
    {
        get => _searchedStockChartEndDate;
        set => SetProperty(ref _searchedStockChartEndDate, value);
    }
    public ObservableCollection<AssetPrice> ConverterCurrencies { get; } = new();
    public ObservableCollection<AssetPrice> Currencies { get; } = new();
    public ObservableCollection<AssetPrice> Cryptos { get; } = new();
    public ObservableCollection<AssetPrice> Stocks { get; } = new();
    public ObservableCollection<AssetPrice> AdditionalCurrencies { get; } = new();
    public ObservableCollection<AssetPrice> SelectedCurrencies { get; } = new();
    public ObservableCollection<AssetPrice> PolishStocks { get; } = new();
    public ObservableCollection<AssetPrice> USStocks { get; } = new();
    public ObservableCollection<AssetPrice> EuropeanStocks { get; } = new();
    public ObservableCollection<AssetPrice> AsianStocks { get; } = new();
    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand LoadCurrenciesCommand { get; }
    public IAsyncRelayCommand LoadCryptosCommand { get; }
    public IAsyncRelayCommand LoadStocksCommand { get; }
    public IRelayCommand ToggleMoreCurrenciesCommand { get; }
    public IAsyncRelayCommand<AssetPrice> SelectAdditionalCurrencyCommand { get; }
    public IRelayCommand SwapCurrenciesCommand { get; }
    public IRelayCommand ToggleFromPickerCommand { get; }
    public IRelayCommand ToggleToPickerCommand { get; }
    public IRelayCommand<AssetPrice> SelectFromCurrencyCommand { get; }
    public IRelayCommand<AssetPrice> SelectToCurrencyCommand { get; }
    public IAsyncRelayCommand SearchCryptoCommand { get; }
    public IAsyncRelayCommand<string> ChangeSearchedChartIntervalCommand { get; }
    public IRelayCommand ToggleAdvancedViewCommand { get; }
    public IAsyncRelayCommand<string> ChangeChartIntervalCommand { get; }
    public IAsyncRelayCommand<AssetPrice> SelectCryptoForChartCommand { get; }
    public IAsyncRelayCommand SearchStockCommand { get; }
    public IAsyncRelayCommand<string> ChangeSearchedStockChartIntervalCommand { get; }
    public MarketsViewModel(
        INbpApiService nbpApiService,
        ICoinCapApiService coinCapApiService,
        IYahooFinanceApiService yahooFinanceApiService)
    {
        _nbpApiService = nbpApiService;
        _coinCapApiService = coinCapApiService;
        _yahooFinanceApiService = yahooFinanceApiService;
        Title = "Rynki";
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadCurrenciesCommand = new AsyncRelayCommand(LoadCurrenciesAsync);
        LoadCryptosCommand = new AsyncRelayCommand(LoadCryptosAsync);
        LoadStocksCommand = new AsyncRelayCommand(LoadStocksAsync);
        ToggleMoreCurrenciesCommand = new RelayCommand(ToggleMoreCurrencies);
        SelectAdditionalCurrencyCommand = new AsyncRelayCommand<AssetPrice>(SelectAdditionalCurrencyAsync);
        SwapCurrenciesCommand = new RelayCommand(SwapCurrencies);
        ToggleFromPickerCommand = new RelayCommand(ToggleFromPicker);
        ToggleToPickerCommand = new RelayCommand(ToggleToPicker);
        SelectFromCurrencyCommand = new RelayCommand<AssetPrice>(SelectFromCurrency);
        SelectToCurrencyCommand = new RelayCommand<AssetPrice>(SelectToCurrency);
        SearchCryptoCommand = new AsyncRelayCommand(SearchCryptoAsync);
        ChangeSearchedChartIntervalCommand = new AsyncRelayCommand<string>(ChangeSearchedChartIntervalAsync);
        ToggleAdvancedViewCommand = new RelayCommand(ToggleAdvancedView);
        ChangeChartIntervalCommand = new AsyncRelayCommand<string>(ChangeChartIntervalAsync);
        SelectCryptoForChartCommand = new AsyncRelayCommand<AssetPrice>(SelectCryptoForChartAsync);
        SearchStockCommand = new AsyncRelayCommand(SearchStockAsync);
        ChangeSearchedStockChartIntervalCommand = new AsyncRelayCommand<string>(ChangeSearchedStockChartIntervalAsync);
        var pln = new AssetPrice
        {
            Symbol = "PLN",
            Name = "Polski zoty",
            PricePln = 1m,
            AssetType = AssetType.CURRENCY
        };
        ConverterCurrencies.Add(pln);
        ConverterFromCurrency = pln;
    }
    private void ToggleAdvancedView()
    {
        IsAdvancedViewEnabled = !IsAdvancedViewEnabled;
        if (IsAdvancedViewEnabled && SelectedCryptoForChart == null && Cryptos.Count > 0)
        {
            var bitcoin = Cryptos.FirstOrDefault(c => c.Symbol == "BTC") ?? Cryptos[0];
            SelectedCryptoForChart = bitcoin;
        }
    }
    private async Task ChangeSearchedChartIntervalAsync(string? interval)
    {
        if (string.IsNullOrEmpty(interval) || SearchedCrypto == null)
            return;
        SearchedChartInterval = interval;
        await LoadSearchedChartDataAsync(SearchedCrypto.Symbol, interval);
    }
    private async Task LoadSearchedChartDataAsync(string symbol, string interval)
    {
        SearchedCryptoChart = new LineChart
        {
            Entries = new[] { new ChartEntry(0) { Color = SKColors.Transparent } },
            BackgroundColor = SKColors.Transparent,
            MinValue = 0,
            MaxValue = 100
        };
        IsSearchedChartLoading = true;
        SearchedChartMinPrice = "";
        SearchedChartMaxPrice = "";
        SearchedChartMidPrice = "";
        SearchedChartStartDate = "";
        SearchedChartMidDate = "";
        SearchedChartEndDate = "";
        await Task.Delay(50);
        try
        {
            var (binanceInterval, limit) = interval switch
            {
                "24h" => ("1h", 24),
                "1tyd." => ("4h", 42),
                "1msc." => ("1d", 30),
                "1rok" => ("1w", 52),
                _ => ("1h", 24)
            };
            var klines = await _coinCapApiService.GetKlinesAsync(symbol, binanceInterval, limit);
            if (klines.Count > 0)
            {
                var minPrice = klines.Min(k => k.Close);
                var maxPrice = klines.Max(k => k.Close);
                var midPrice = (minPrice + maxPrice) / 2;
                SearchedChartMaxPrice = $"${maxPrice:N0}";
                SearchedChartMidPrice = $"${midPrice:N0}";
                SearchedChartMinPrice = $"${minPrice:N0}";
                var firstKline = klines.First();
                var lastKline = klines.Last();
                var midIndex = klines.Count / 2;
                var midKline = klines[midIndex];
                SearchedChartStartDate = firstKline.OpenTime.ToString("dd.MM");
                SearchedChartMidDate = midKline.OpenTime.ToString("dd.MM");
                SearchedChartEndDate = lastKline.OpenTime.ToString("dd.MM");
                var isUptrend = klines.Last().Close >= klines.First().Close;
                var lineColor = isUptrend ? SKColor.Parse("#4CAF50") : SKColor.Parse("#F44336");
                var entries = klines.Select((k, index) => new ChartEntry((float)k.Close)
                {
                    Color = lineColor
                }).ToArray();
                var newChart = new LineChart
                {
                    Entries = entries,
                    LineMode = LineMode.Spline,
                    LineSize = 3,
                    PointMode = PointMode.None,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    MinValue = (float)(minPrice * 0.995m),
                    MaxValue = (float)(maxPrice * 1.005m),
                    AnimationDuration = TimeSpan.Zero
                };
                IsSearchedChartLoading = false;
                SearchedCryptoChart = newChart;
                return;
            }
        }
        catch (Exception)
        {
            SearchedCryptoChart = null;
        }
        IsSearchedChartLoading = false;
    }
    private async Task SearchCryptoAsync()
    {
        SearchedCrypto = null;
        SearchedCryptoChart = null;
        CryptoSearchError = null;
        SearchedChartInterval = "24h";
        var query = CryptoSearchQuery?.Trim().ToUpper();
        if (string.IsNullOrEmpty(query))
        {
            CryptoSearchError = "Wpisz symbol kryptowaluty (np. BTC, ETH, DOGE)";
            return;
        }
        IsCryptoSearching = true;
        try
        {
            var result = await _coinCapApiService.GetCryptoAssetWithDetailsAsync(query);
            if (result != null)
            {
                SearchedCrypto = result;
                _ = LoadSearchedChartDataAsync(result.Symbol, SearchedChartInterval);
            }
            else
            {
                CryptoSearchError = "Przykro nam, ale niestety podana kryptowaluta nie znajduje się w naszej bazie.";
            }
        }
        catch (Exception)
        {
            CryptoSearchError = "Przykro nam, ale niestety podana kryptowaluta nie znajduje się w naszej bazie.";
        }
        finally
        {
            IsCryptoSearching = false;
        }
    }
    private async Task ChangeChartIntervalAsync(string? interval)
    {
        if (string.IsNullOrEmpty(interval) || SelectedCryptoForChart == null)
            return;
        SelectedChartInterval = interval;
        await LoadChartDataAsync(SelectedCryptoForChart.Symbol, interval);
    }
    private async Task SelectCryptoForChartAsync(AssetPrice? crypto)
    {
    }
    private async Task LoadChartDataAsync(string symbol, string interval)
    {
        CryptoChart = new LineChart
        {
            Entries = new[] { new ChartEntry(0) { Color = SKColors.Transparent } },
            BackgroundColor = SKColors.Transparent,
            MinValue = 0,
            MaxValue = 100
        };
        IsChartLoading = true;
        ChartMinPrice = "";
        ChartMaxPrice = "";
        ChartMidPrice = "";
        ChartStartDate = "";
        ChartMidDate = "";
        ChartEndDate = "";
        await Task.Delay(50);
        try
        {
            var (binanceInterval, limit) = interval switch
            {
                "24h" => ("1h", 24),
                "1tyd." => ("4h", 42),
                "1msc." => ("1d", 30),
                "1rok" => ("1w", 52),
                _ => ("1h", 24)
            };
            var klines = await _coinCapApiService.GetKlinesAsync(symbol, binanceInterval, limit);
            if (klines.Count > 0)
            {
                var minPrice = klines.Min(k => k.Close);
                var maxPrice = klines.Max(k => k.Close);
                var midPrice = (minPrice + maxPrice) / 2;
                ChartMaxPrice = $"${maxPrice:N0}";
                ChartMidPrice = $"${midPrice:N0}";
                ChartMinPrice = $"${minPrice:N0}";
                var firstKline = klines.First();
                var lastKline = klines.Last();
                var midIndex = klines.Count / 2;
                var midKline = klines[midIndex];
                ChartStartDate = firstKline.OpenTime.ToString("dd.MM");
                ChartMidDate = midKline.OpenTime.ToString("dd.MM");
                ChartEndDate = lastKline.OpenTime.ToString("dd.MM");
                var isUptrend = klines.Last().Close >= klines.First().Close;
                var lineColor = isUptrend ? SKColor.Parse("#4CAF50") : SKColor.Parse("#F44336");
                var entries = klines.Select((k, index) => new ChartEntry((float)k.Close)
                {
                    Color = lineColor
                }).ToArray();
                var newChart = new LineChart
                {
                    Entries = entries,
                    LineMode = LineMode.Spline,
                    LineSize = 3,
                    PointMode = PointMode.None,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    MinValue = (float)(minPrice * 0.995m),
                    MaxValue = (float)(maxPrice * 1.005m),
                    AnimationDuration = TimeSpan.Zero
                };
                IsChartLoading = false;
                CryptoChart = newChart;
                return;
            }
        }
        catch (Exception)
        {
            CryptoChart = null;
        }
        IsChartLoading = false;
    }
    private async Task LoadDataAsync()
    {
#if ANDROID
        await LoadCurrenciesAsync();
        await LoadCryptosAsync();
        await LoadStocksAsync();
#else
        await Task.WhenAll(
            LoadCurrenciesAsync(),
            LoadCryptosAsync(),
            LoadStocksAsync()
        );
#endif
    }
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync();
        IsRefreshing = false;
    }
    private void CalculateConversion()
    {
        if (ConverterFromCurrency == null || ConverterToCurrency == null)
        {
            ConverterToAmount = "";
            return;
        }
        if (!decimal.TryParse(ConverterFromAmount.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount))
        {
            ConverterToAmount = "";
            return;
        }
        var amountInPln = amount * ConverterFromCurrency.PricePln;
        var result = amountInPln / ConverterToCurrency.PricePln;
        ConverterToAmount = result.ToString("N4");
    }
    private void SwapCurrencies()
    {
        var tempCurrency = ConverterFromCurrency;
        var tempAmount = ConverterFromAmount;
        ConverterFromCurrency = ConverterToCurrency;
        ConverterFromAmount = ConverterToAmount;
        ConverterToCurrency = tempCurrency;
    }
    private void ToggleFromPicker()
    {
        IsConverterFromPickerVisible = !IsConverterFromPickerVisible;
        if (IsConverterFromPickerVisible)
            IsConverterToPickerVisible = false;
    }
    private void ToggleToPicker()
    {
        IsConverterToPickerVisible = !IsConverterToPickerVisible;
        if (IsConverterToPickerVisible)
            IsConverterFromPickerVisible = false;
    }
    private void SelectFromCurrency(AssetPrice? currency)
    {
        if (currency != null)
        {
            ConverterFromCurrency = currency;
            IsConverterFromPickerVisible = false;
        }
    }
    private void SelectToCurrency(AssetPrice? currency)
    {
        if (currency != null)
        {
            ConverterToCurrency = currency;
            IsConverterToPickerVisible = false;
        }
    }
    private async Task UpdateConverterCurrenciesAsync()
    {
        var currentFrom = ConverterFromCurrency;
        var currentTo = ConverterToCurrency;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConverterCurrencies.Clear();
            var pln = new AssetPrice
            {
                Symbol = "PLN",
                Name = "Polski złoty",
                PricePln = 1m,
                AssetType = AssetType.CURRENCY
            };
            ConverterCurrencies.Add(pln);
            foreach (var currency in Currencies)
            {
                ConverterCurrencies.Add(currency);
            }
        });
        try
        {
            var allCurrencies = await _nbpApiService.GetAllAvailableCurrenciesAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var currency in allCurrencies)
                {
                    if (!ConverterCurrencies.Any(c => c.Symbol == currency.Symbol))
                    {
                        ConverterCurrencies.Add(currency);
                    }
                }
            });
        }
        catch (Exception)
        {
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var pln = new AssetPrice { Symbol = "PLN", Name = "Polski złoty", PricePln = 1m, AssetType = AssetType.CURRENCY };
            if (currentFrom != null)
                ConverterFromCurrency = ConverterCurrencies.FirstOrDefault(c => c.Symbol == currentFrom.Symbol) ?? pln;
            if (currentTo != null)
                ConverterToCurrency = ConverterCurrencies.FirstOrDefault(c => c.Symbol == currentTo.Symbol);
        });
    }
    private async Task LoadCurrenciesAsync()
    {
        IsCurrenciesLoading = true;
        CurrenciesError = null;
        try
        {
            var currencies = await _nbpApiService.GetAllCurrencyRatesAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Currencies.Clear();
                foreach (var currency in currencies)
                    Currencies.Add(currency);
            });
            await UpdateConverterCurrenciesAsync();
        }
        catch (HttpRequestException)
        {
            CurrenciesError = "Błąd połączenia z serwerem walut.";
        }
        catch (Exception ex)
        {
            CurrenciesError = ex.Message;
        }
        finally
        {
            IsCurrenciesLoading = false;
        }
    }
    private async Task LoadCryptosAsync()
    {
        IsCryptosLoading = true;
        CryptosError = null;
        try
        {
            var cryptos = await _coinCapApiService.GetAllCryptoAssetsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Cryptos.Clear();
                foreach (var crypto in cryptos)
                    Cryptos.Add(crypto);
            });
        }
        catch (HttpRequestException)
        {
            CryptosError = "Błąd połączenia z serwerem kryptowalut.";
        }
        catch (Exception ex)
        {
            CryptosError = ex.Message;
        }
        finally
        {
            IsCryptosLoading = false;
        }
    }
    private async Task LoadStocksAsync()
    {
        IsStocksLoading = true;
        StocksError = null;
        try
        {
#if ANDROID
            var polishStocks = await _yahooFinanceApiService.GetPolishStocksAsync();
            var usStocks = await _yahooFinanceApiService.GetUSStocksAsync();
            var europeanStocks = await _yahooFinanceApiService.GetEuropeanStocksAsync();
            var asianStocks = await _yahooFinanceApiService.GetAsianStocksAsync();
#else
            var polishTask = _yahooFinanceApiService.GetPolishStocksAsync();
            var usTask = _yahooFinanceApiService.GetUSStocksAsync();
            var europeanTask = _yahooFinanceApiService.GetEuropeanStocksAsync();
            var asianTask = _yahooFinanceApiService.GetAsianStocksAsync();
            await Task.WhenAll(polishTask, usTask, europeanTask, asianTask);
            var polishStocks = await polishTask;
            var usStocks = await usTask;
            var europeanStocks = await europeanTask;
            var asianStocks = await asianTask;
#endif
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PolishStocks.Clear();
                foreach (var stock in polishStocks)
                    PolishStocks.Add(stock);
                USStocks.Clear();
                foreach (var stock in usStocks)
                    USStocks.Add(stock);
                EuropeanStocks.Clear();
                foreach (var stock in europeanStocks)
                    EuropeanStocks.Add(stock);
                AsianStocks.Clear();
                foreach (var stock in asianStocks)
                    AsianStocks.Add(stock);
                Stocks.Clear();
                foreach (var stock in polishStocks.Concat(usStocks).Concat(europeanStocks).Concat(asianStocks))
                    Stocks.Add(stock);
            });
        }
        catch (HttpRequestException)
        {
            StocksError = "Błąd połączenia z serwerem giełdowym.";
        }
        catch (Exception ex)
        {
            StocksError = ex.Message;
        }
        finally
        {
            IsStocksLoading = false;
        }
    }
    private void ToggleMoreCurrencies()
    {
        IsMoreCurrenciesExpanded = !IsMoreCurrenciesExpanded;
        if (IsMoreCurrenciesExpanded && AdditionalCurrencies.Count == 0)
        {
            _ = LoadAdditionalCurrenciesAsync();
        }
    }
    private async Task LoadAdditionalCurrenciesAsync()
    {
        IsMoreCurrenciesLoading = true;
        try
        {
            var currencies = await _nbpApiService.GetAllAvailableCurrenciesAsync();
            AdditionalCurrencies.Clear();
            foreach (var currency in currencies)
            {
                if (!SelectedCurrencies.Any(c => c.Symbol == currency.Symbol))
                {
                    AdditionalCurrencies.Add(currency);
                }
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            IsMoreCurrenciesLoading = false;
        }
    }
    private async Task SelectAdditionalCurrencyAsync(AssetPrice? currency)
    {
        if (currency == null)
            return;
        if (!SelectedCurrencies.Any(c => c.Symbol == currency.Symbol))
        {
            SelectedCurrencies.Add(currency);
        }
        var toRemove = AdditionalCurrencies.FirstOrDefault(c => c.Symbol == currency.Symbol);
        if (toRemove != null)
        {
            AdditionalCurrencies.Remove(toRemove);
        }
        IsMoreCurrenciesExpanded = false;
    }
    private async Task SearchStockAsync()
    {
        SearchedStock = null;
        SearchedStockChart = null;
        StockSearchError = null;
        SearchedStockChartInterval = "1msc.";
        var query = StockSearchQuery?.Trim().ToUpper();
        if (string.IsNullOrEmpty(query))
        {
            StockSearchError = "Wpisz symbol giełdowy (np. AAPL, MSFT, PKO.WA)";
            return;
        }
        IsStockSearching = true;
        try
        {
            var result = await _yahooFinanceApiService.SearchStockAsync(query);
            if (result != null)
            {
                SearchedStock = result;
                _ = LoadSearchedStockChartDataAsync(result.Symbol, SearchedStockChartInterval);
            }
            else
            {
                StockSearchError = "Nie znaleziono firmy o podanym symbolu. Spróbuj innego symbolu giełdowego.";
            }
        }
        catch (Exception)
        {
            StockSearchError = "Wystpił błąd podczas wyszukiwania. Spróbuj ponownie później.";
        }
        finally
        {
            IsStockSearching = false;
        }
    }
    private async Task ChangeSearchedStockChartIntervalAsync(string? interval)
    {
        if (string.IsNullOrEmpty(interval) || SearchedStock == null)
            return;
        SearchedStockChartInterval = interval;
        await LoadSearchedStockChartDataAsync(SearchedStock.Symbol, interval);
    }
    private async Task LoadSearchedStockChartDataAsync(string symbol, string interval)
    {
        SearchedStockChart = new LineChart
        {
            Entries = new[] { new ChartEntry(0) { Color = SKColors.Transparent } },
            BackgroundColor = SKColors.Transparent,
            MinValue = 0,
            MaxValue = 100
        };
        IsSearchedStockChartLoading = true;
        SearchedStockChartMinPrice = "";
        SearchedStockChartMaxPrice = "";
        SearchedStockChartMidPrice = "";
        SearchedStockChartStartDate = "";
        SearchedStockChartMidDate = "";
        SearchedStockChartEndDate = "";
        await Task.Delay(50);
        try
        {
            var (yahooInterval, limit) = interval switch
            {
                "24h" => ("1h", 24),
                "1tyd." => ("4h", 42),
                "1msc." => ("1d", 30),
                "1rok" => ("1w", 52),
                _ => ("1d", 30)
            };
            var klines = await _yahooFinanceApiService.GetStockHistoryAsync(symbol, yahooInterval, limit);
            if (klines.Count > 0)
            {
                var minPrice = klines.Min(k => k.Close);
                var maxPrice = klines.Max(k => k.Close);
                var midPrice = (minPrice + maxPrice) / 2;
                var currencySymbol = SearchedStock?.OriginalCurrency == "PLN" ? "z" : "$";
                SearchedStockChartMaxPrice = $"{currencySymbol}{maxPrice:N2}";
                SearchedStockChartMidPrice = $"{currencySymbol}{midPrice:N2}";
                SearchedStockChartMinPrice = $"{currencySymbol}{minPrice:N2}";
                var firstKline = klines.First();
                var lastKline = klines.Last();
                var midIndex = klines.Count / 2;
                var midKline = klines[midIndex];
                SearchedStockChartStartDate = firstKline.OpenTime.ToString("dd.MM");
                SearchedStockChartMidDate = midKline.OpenTime.ToString("dd.MM");
                SearchedStockChartEndDate = lastKline.OpenTime.ToString("dd.MM");
                var isUptrend = klines.Last().Close >= klines.First().Close;
                var lineColor = isUptrend ? SKColor.Parse("#4CAF50") : SKColor.Parse("#F44336");
                var entries = klines.Select((k, index) => new ChartEntry((float)k.Close)
                {
                    Color = lineColor
                }).ToArray();
                var newChart = new LineChart
                {
                    Entries = entries,
                    LineMode = LineMode.Spline,
                    LineSize = 3,
                    PointMode = PointMode.None,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    MinValue = (float)(minPrice * 0.995m),
                    MaxValue = (float)(maxPrice * 1.005m),
                    AnimationDuration = TimeSpan.Zero
                };
                IsSearchedStockChartLoading = false;
                SearchedStockChart = newChart;
                return;
            }
        }
        catch (Exception)
        {
            SearchedStockChart = null;
        }
        IsSearchedStockChartLoading = false;
    }
}