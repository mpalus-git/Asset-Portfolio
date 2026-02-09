using PortfelStudenta.ViewModels;
namespace PortfelStudenta.Views;
public partial class MarketsPage : ContentPage
{
    public MarketsPage(MarketsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MarketsViewModel vm)
        {
            Task.Run(() => vm.LoadDataCommand.Execute(null));
        }
    }
}