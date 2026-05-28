using CommunityToolkit.Mvvm.ComponentModel;

namespace ZoroDBRestore.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected void SetStatus(string message) => StatusMessage = message;

    protected void SetBusy(bool busy, string? message = null)
    {
        IsBusy = busy;
        if (message is not null) StatusMessage = message;
    }
}
