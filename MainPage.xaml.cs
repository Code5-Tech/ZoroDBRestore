namespace ZoroDBRestore;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnBackupClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//backup");

    private async void OnRestoreClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//restore");

    private async void OnConnectionsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//connections");
}
