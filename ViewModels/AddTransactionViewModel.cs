using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using PortfelStudenta.Models;
using PortfelStudenta.Services;
namespace PortfelStudenta.ViewModels;
public class AddTransactionViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly INbpApiService _nbpApiService;
    private readonly ICoinCapApiService _coinCapApiService;
    private readonly IYahooFinanceApiService _yahooFinanceApiService;
    private int _selectedAssetTypeIndex;
    private int _selectedTransactionTypeIndex;
    private string _symbol = string.Empty;
    private string _name = string.Empty;
    private string _quantityText = string.Empty;
    private string _unitPriceText = string.Empty;
    private string _commissionText = "0";
    private string _commissionCurrency = "PLN";
    private DateTime _transactionDate = DateTime.Now;
    private string _notes = string.Empty;
    private string? _symbolError;
    private string? _quantityError;
    private string? _priceError;
    private bool _canSave;
    private decimal _totalValue;
    public int SelectedAssetTypeIndex
    {
        get => _selectedAssetTypeIndex;
        set
        {
            if (SetProperty(ref _selectedAssetTypeIndex, value))
            {
                UpdateSuggestedAssets();
                ValidateForm();
            }
        }
    }
    public int SelectedTransactionTypeIndex
    {
        get => _selectedTransactionTypeIndex;
        set => SetProperty(ref _selectedTransactionTypeIndex, value);
    }
    public string Symbol
    {
        get => _symbol;
        set
        {
            if (SetProperty(ref _symbol, value))
            {
                ValidateSymbol();
                ValidateForm();
            }
        }
    }
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (SetProperty(ref _quantityText, value))
            {
                ValidateQuantity();
                CalculateTotalValue();
                ValidateForm();
            }
        }
    }
    public string UnitPriceText
    {
        get => _unitPriceText;
        set
        {
            if (SetProperty(ref _unitPriceText, value))
            {
                ValidatePrice();
                CalculateTotalValue();
                ValidateForm();
            }
        }
    }
    public string CommissionText
    {
        get => _commissionText;
        set => SetProperty(ref _commissionText, value);
    }
    public string CommissionCurrency
    {
        get => _commissionCurrency;
        set => SetProperty(ref _commissionCurrency, value);
    }
    public DateTime TransactionDate
    {
        get => _transactionDate;
        set => SetProperty(ref _transactionDate, value);
    }
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }
    public string? SymbolError
    {
        get => _symbolError;
        set => SetProperty(ref _symbolError, value);
    }
    public string? QuantityError
    {
        get => _quantityError;
        set => SetProperty(ref _quantityError, value);
    }
    public string? PriceError
    {
        get => _priceError;
        set => SetProperty(ref _priceError, value);
    }
    public bool CanSave
    {
        get => _canSave;
        set => SetProperty(ref _canSave, value);
    }
    public decimal TotalValue
    {
        get => _totalValue;
        set => SetProperty(ref _totalValue, value);
    }
    public ObservableCollection<string> SuggestedAssets { get; } = new();
    public List<string> AssetTypes { get; } = new() { "Kryptowaluta", "Akcje", "Waluta" };
    public List<string> TransactionTypes { get; } = new() { "Kupno", "Sprzedaż" };
    public List<string> Currencies { get; } = new() { "PLN", "USD", "EUR" };
    public IRelayCommand<string> SelectAssetCommand { get; }
    public IAsyncRelayCommand FetchCurrentPriceCommand { get; }
    public IAsyncRelayCommand SaveTransactionCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public AddTransactionViewModel(
        IDatabaseService databaseService,
        INbpApiService nbpApiService,
        ICoinCapApiService coinCapApiService,
        IYahooFinanceApiService yahooFinanceApiService)
    {
        _databaseService = databaseService;
        _nbpApiService = nbpApiService;
        _coinCapApiService = coinCapApiService;
        _yahooFinanceApiService = yahooFinanceApiService;
        Title = "Dodaj transakcję";
        SelectAssetCommand = new RelayCommand<string>(SelectAsset);
        FetchCurrentPriceCommand = new AsyncRelayCommand(FetchCurrentPriceAsync);
        SaveTransactionCommand = new AsyncRelayCommand(SaveTransactionAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        UpdateSuggestedAssets();
    }
    private void SelectAsset(string? asset)
    {
        if (asset == null) return;
        Symbol = asset;
        Name = GetAssetName(asset);
    }
    private async Task FetchCurrentPriceAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
            return;
        await ExecuteBusyAsync(async () =>
        {
            var assetType = (AssetType)SelectedAssetTypeIndex;
            AssetPrice? price = null;
            try
            {
                price = assetType switch
                {
                    AssetType.CURRENCY => await _nbpApiService.GetCurrencyRateAsync(Symbol),
                    AssetType.CRYPTO => await GetCryptoPriceAsync(Symbol),
                    AssetType.STOCK => await _yahooFinanceApiService.GetStockPriceAsync(Symbol),
                    _ => null
                };
            }
            catch (Exception)
            {
            }
            if (price != null)
            {
                UnitPriceText = price.PricePln.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(Name))
                    Name = price.Name;
            }
        });
    }
    private async Task SaveTransactionAsync()
    {
        if (!ValidateForm())
            return;
        await ExecuteBusyAsync(async () =>
        {
            var normalizedQty = QuantityText.Replace(" ", "").Replace(",", ".");
            var normalizedPrice = UnitPriceText.Replace(" ", "").Replace(",", ".");
            var normalizedCommission = CommissionText?.Replace(" ", "").Replace(",", ".") ?? "0";
            var transaction = new Transaction
            {
                AssetType = (AssetType)SelectedAssetTypeIndex,
                TransactionType = (TransactionType)SelectedTransactionTypeIndex,
                Symbol = Symbol.ToUpper().Trim(),
                Name = string.IsNullOrWhiteSpace(Name) ? Symbol.ToUpper() : Name.Trim(),
                Quantity = decimal.Parse(normalizedQty, System.Globalization.CultureInfo.InvariantCulture),
                UnitPrice = decimal.Parse(normalizedPrice, System.Globalization.CultureInfo.InvariantCulture),
                TotalValue = TotalValue,
                Commission = string.IsNullOrWhiteSpace(normalizedCommission) ? 0 : decimal.Parse(normalizedCommission, System.Globalization.CultureInfo.InvariantCulture),
                CommissionCurrency = CommissionCurrency,
                Date = TransactionDate,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };
            await _databaseService.SaveTransactionAsync(transaction);
            await Shell.Current.GoToAsync("..");
        });
    }
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
    private void UpdateSuggestedAssets()
    {
        SuggestedAssets.Clear();
        var assets = SelectedAssetTypeIndex switch
        {
            0 => new[] { "BTC", "ETH", "BNB", "XRP", "ADA", "SOL", "DOT", "DOGE" },
            1 => new[] { "CDR.WA", "PKO.WA", "PKN.WA", "PZU.WA", "AAPL", "MSFT", "GOOGL", "AMZN" },
            2 => new[] { "EUR", "USD", "GBP", "CHF" },
            _ => Array.Empty<string>()
        };
        foreach (var asset in assets)
            SuggestedAssets.Add(asset);
    }
    private void ValidateSymbol()
    {
        SymbolError = string.IsNullOrWhiteSpace(Symbol) ? "Symbol jest wymagany" : null;
    }
    private void ValidateQuantity()
    {
        if (string.IsNullOrWhiteSpace(QuantityText))
        {
            QuantityError = "Ilość jest wymagana";
            return;
        }
        if (!decimal.TryParse(QuantityText.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            QuantityError = "Ilość musi być większa od 0";
            return;
        }
        QuantityError = null;
    }
    private void ValidatePrice()
    {
        if (string.IsNullOrWhiteSpace(UnitPriceText))
        {
            PriceError = "Cena jest wymagana";
            return;
        }
        var normalizedPrice = UnitPriceText.Replace(" ", "").Replace(",", ".");
        if (!decimal.TryParse(normalizedPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            PriceError = "Cena musi być większa od 0";
            return;
        }
        PriceError = null;
    }
    private bool ValidateForm()
    {
        ValidateSymbol();
        ValidateQuantity();
        ValidatePrice();
        CanSave = string.IsNullOrEmpty(SymbolError) &&
                  string.IsNullOrEmpty(QuantityError) &&
                  string.IsNullOrEmpty(PriceError);
        return CanSave;
    }
    private void CalculateTotalValue()
    {
        var normalizedQty = QuantityText?.Replace(" ", "").Replace(",", ".");
        var normalizedPrice = UnitPriceText?.Replace(" ", "").Replace(",", ".");
        if (decimal.TryParse(normalizedQty, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var qty) &&
            decimal.TryParse(normalizedPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            TotalValue = qty * price;
        }
        else
        {
            TotalValue = 0;
        }
    }
    private async Task<AssetPrice?> GetCryptoPriceAsync(string symbol)
    {
        return await _coinCapApiService.GetCryptoAssetAsync(symbol.ToUpper());
    }
    private string GetAssetName(string symbol) => symbol.ToUpper() switch
    {
        "BTC" => "Bitcoin",
        "ETH" => "Ethereum",
        "BNB" => "Binance Coin",
        "XRP" => "Ripple",
        "ADA" => "Cardano",
        "SOL" => "Solana",
        "DOT" => "Polkadot",
        "DOGE" => "Dogecoin",
        "EUR" => "Euro",
        "USD" => "Dolar amerykański",
        "GBP" => "Funt brytyjski",
        "CHF" => "Frank szwajcarski",
        "CDR.WA" => "CD Projekt",
        "PKO.WA" => "PKO Bank Polski",
        "PKN.WA" => "PKN Orlen",
        "PZU.WA" => "PZU",
        "AAPL" => "Apple Inc.",
        "MSFT" => "Microsoft Corp.",
        "GOOGL" => "Alphabet Inc.",
        "AMZN" => "Amazon.com Inc.",
        _ => symbol
    };
}