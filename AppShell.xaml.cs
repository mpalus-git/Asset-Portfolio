using PortfelStudenta.Views;
namespace PortfelStudenta
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("AddTransactionPage", typeof(AddTransactionPage));
        }
    }
}