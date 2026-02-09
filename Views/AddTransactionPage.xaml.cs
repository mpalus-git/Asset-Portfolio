using PortfelStudenta.ViewModels;
namespace PortfelStudenta.Views;
public partial class AddTransactionPage : ContentPage
{
    public AddTransactionPage(AddTransactionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}