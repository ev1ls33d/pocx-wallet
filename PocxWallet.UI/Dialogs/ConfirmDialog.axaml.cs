using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PocxWallet.UI.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message, string confirmText = "Yes", string cancelText = "No") : this()
    {
        DataContext = new ConfirmDialogViewModel(title, message, confirmText, cancelText, this);
    }

    public bool? DialogResult => (DataContext as ConfirmDialogViewModel)?.DialogResult;
}

public partial class ConfirmDialogViewModel : ObservableObject
{
    private readonly Window _window;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _message;

    [ObservableProperty]
    private string _confirmText;

    [ObservableProperty]
    private string _cancelText;

    public bool? DialogResult { get; private set; }

    public ConfirmDialogViewModel(string title, string message, string confirmText, string cancelText, Window window)
    {
        _title = title;
        _message = message;
        _confirmText = confirmText;
        _cancelText = cancelText;
        _window = window;
    }

    [RelayCommand]
    private void Confirm()
    {
        DialogResult = true;
        _window.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        _window.Close();
    }
}
