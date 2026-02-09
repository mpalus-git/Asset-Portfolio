using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
namespace PortfelStudenta.ViewModels;
public class BaseViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _title = string.Empty;
    private bool _isRefreshing;
    private string? _errorMessage;
    private bool _hasError;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(IsNotBusy));
        }
    }
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }
    public bool IsNotBusy => !IsBusy;
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
    protected void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }
    protected async Task ExecuteBusyAsync(Func<Task> action)
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            ClearError();
            await action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{GetType().Name}] {ex}");
            var userMessage = ex switch
            {
                InvalidOperationException => ex.Message,
                TimeoutException => ex.Message,
                _ => "Wyst¹pi³ nieoczekiwany b³¹d. Spróbuj ponownie."
            };
            ShowError(userMessage);
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}