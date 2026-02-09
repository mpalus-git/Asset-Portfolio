using PortfelStudenta.ViewModels;
namespace PortfelStudenta.Views;
public partial class PortfolioPage : ContentPage
{
    public PortfolioPage(PortfolioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PortfolioViewModel vm)
        {
            vm.LoadDataCommand.Execute(null);
        }
    }
}