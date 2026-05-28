using ZoroDBRestore.ViewModels;

namespace ZoroDBRestore.Views;

public partial class RestorePage : ContentPage
{
    private readonly RestoreViewModel _vm;

    public RestorePage(RestoreViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshProfiles();
    }
}
