using ZoroDBRestore.ViewModels;

namespace ZoroDBRestore.Views;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel _vm;

    public BackupPage(BackupViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Refresh profiles in case the user added/edited one in the Connections tab
        _vm.RefreshProfiles();
    }
}
