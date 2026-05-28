using ZoroDBRestore.ViewModels;

namespace ZoroDBRestore.Views;

public partial class ConnectionsPage : ContentPage
{
    public ConnectionsPage(ConnectionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
