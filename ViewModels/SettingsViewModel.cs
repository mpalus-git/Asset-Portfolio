using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;
using PortfelStudenta.Services;
using System.Text;
namespace PortfelStudenta.ViewModels;
public class SettingsViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ICsvService _csvService;
    private readonly IFileSaver _fileSaver;
    private int _transactionCount;
    private string? _importResult;
    private bool _showImportResult;
    public int TransactionCount
    {
        get => _transactionCount;
        set => SetProperty(ref _transactionCount, value);
    }
    public string? ImportResult
    {
        get => _importResult;
        set => SetProperty(ref _importResult, value);
    }
    public bool ShowImportResult
    {
        get => _showImportResult;
        set => SetProperty(ref _showImportResult, value);
    }
    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand ExportToCsvCommand { get; }
    public IAsyncRelayCommand ImportFromCsvCommand { get; }
    public IAsyncRelayCommand ClearAllDataCommand { get; }
    public SettingsViewModel(IDatabaseService databaseService, ICsvService csvService, IFileSaver fileSaver)
    {
        _databaseService = databaseService;
        _csvService = csvService;
        _fileSaver = fileSaver;
        Title = "Ustawienia";
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        ExportToCsvCommand = new AsyncRelayCommand(ExportToCsvAsync);
        ImportFromCsvCommand = new AsyncRelayCommand(ImportFromCsvAsync);
        ClearAllDataCommand = new AsyncRelayCommand(ClearAllDataAsync);
    }
    private async Task LoadDataAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            TransactionCount = await _databaseService.GetTransactionCountAsync();
        });
    }
    private async Task ExportToCsvAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            try
            {
                var csvContent = await _csvService.ExportTransactionsAsync();
                var fileName = $"PortfelStudenta_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
                var result = await _fileSaver.SaveAsync(fileName, stream, CancellationToken.None);
                if (result.IsSuccessful)
                {
                    if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
                    {
                        await page.DisplayAlertAsync("Sukces", $"Plik został zapisany: {result.FilePath}", "OK");
                    }
                }
                else if (result.Exception != null)
                {
                    ShowError($"Błąd eksportu: {result.Exception.Message}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Błąd eksportu: {ex.Message}");
            }
        });
    }
    private async Task ImportFromCsvAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik CSV",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
                        { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values" } },
                        { DevicePlatform.WinUI, new[] { ".csv" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text" } }
                    })
                });
                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    var importResult = await _csvService.ImportTransactionsAsync(content);
                    var message = $"Zaimportowano: {importResult.SuccessCount} transakcji\n" +
                                  $"Błędy: {importResult.ErrorCount}";
                    if (importResult.Errors.Any())
                    {
                        message += $"\n\nPierwsze błędy:\n{string.Join("\n", importResult.Errors.Take(5))}";
                    }
                    ImportResult = message;
                    ShowImportResult = true;
                    await LoadDataAsync();
                    if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
                    {
                        await page.DisplayAlertAsync("Import zakończony", message, "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Błąd importu: {ex.Message}");
            }
        });
    }
    private async Task ClearAllDataAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
        {
            var confirm = await page.DisplayAlertAsync(
                "Wyczyść dane",
                "Czy na pewno chcesz usunąć wszystkie transakcje? Ta operacja jest nieodwracalna.",
                "Usuń wszystko", "Anuluj");
            if (confirm)
            {
                await ExecuteBusyAsync(async () =>
                {
                    await _databaseService.DeleteAllTransactionsAsync();
                    await LoadDataAsync();
                    await page.DisplayAlertAsync("Sukces", "Wszystkie dane zostały usunięte.", "OK");
                });
            }
        }
    }
}