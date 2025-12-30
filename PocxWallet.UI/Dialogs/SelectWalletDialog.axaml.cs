using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Models;
using PocxWallet.UI.Services;
using System.Collections.ObjectModel;

namespace PocxWallet.UI.Dialogs;

public partial class SelectWalletDialog : Window
{
    public SelectWalletDialog()
    {
        InitializeComponent();
    }

    public SelectWalletDialog(IWalletService walletService) : this()
    {
        DataContext = new SelectWalletDialogViewModel(walletService, this);
    }

    public string? SelectedWalletName => (DataContext as SelectWalletDialogViewModel)?.SelectedWallet?.Name;
    public bool? DialogResult => (DataContext as SelectWalletDialogViewModel)?.DialogResult;
}

public class SelectableWalletEntry
{
    public string Name { get; set; } = "";
    public string MainnetAddress { get; set; } = "";
    public bool IsActive { get; set; }
}

public partial class SelectWalletDialogViewModel : ObservableObject
{
    private readonly IWalletService _walletService;
    private readonly Window _window;

    public ObservableCollection<SelectableWalletEntry> Wallets { get; } = new();

    [ObservableProperty]
    private SelectableWalletEntry? _selectedWallet;

    public bool CanSelect => SelectedWallet != null;

    public bool? DialogResult { get; private set; }

    public SelectWalletDialogViewModel(IWalletService walletService, Window window)
    {
        _walletService = walletService;
        _window = window;

        var activeWallet = walletService.GetActiveWallet();
        var allWallets = walletService.GetAllWallets();

        foreach (var wallet in allWallets)
        {
            var entry = new SelectableWalletEntry
            {
                Name = wallet.Name,
                MainnetAddress = wallet.MainnetAddress,
                IsActive = wallet.Name == activeWallet?.Name
            };
            Wallets.Add(entry);
            
            if (entry.IsActive)
                SelectedWallet = entry;
        }
    }

    partial void OnSelectedWalletChanged(SelectableWalletEntry? value)
    {
        OnPropertyChanged(nameof(CanSelect));
    }

    [RelayCommand]
    private void Select()
    {
        if (SelectedWallet != null)
        {
            _walletService.SwitchWallet(SelectedWallet.Name);
            DialogResult = true;
            _window.Close();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        _window.Close();
    }
}
