using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;

namespace PocxWallet.UI.Dialogs;

public partial class CreateWalletDialog : Window
{
    public CreateWalletDialog()
    {
        InitializeComponent();
    }

    public CreateWalletDialog(IWalletService walletService) : this()
    {
        DataContext = new CreateWalletDialogViewModel(walletService, this);
    }
}

public partial class CreateWalletDialogViewModel : ObservableObject
{
    private readonly IWalletService _walletService;
    private readonly Window _window;

    [ObservableProperty]
    private string _walletName = "default";

    [ObservableProperty]
    private string _passphrase = "";

    [ObservableProperty]
    private string _walletNameError = "";

    [ObservableProperty]
    private bool _hasWalletNameError;

    [ObservableProperty]
    private bool _isCreated;

    [ObservableProperty]
    private string _mnemonic = "";

    [ObservableProperty]
    private string _mainnetAddress = "";

    [ObservableProperty]
    private string _testnetAddress = "";

    public bool? DialogResult { get; private set; }

    public CreateWalletDialogViewModel(IWalletService walletService, Window window)
    {
        _walletService = walletService;
        _window = window;
    }

    [RelayCommand]
    private void Create()
    {
        // Validate wallet name
        if (string.IsNullOrWhiteSpace(WalletName))
        {
            WalletNameError = "Wallet name is required.";
            HasWalletNameError = true;
            return;
        }

        if (_walletService.WalletNameExists(WalletName))
        {
            WalletNameError = $"Wallet '{WalletName}' already exists.";
            HasWalletNameError = true;
            return;
        }

        HasWalletNameError = false;

        try
        {
            var passphrase = string.IsNullOrEmpty(Passphrase) ? null : Passphrase;
            var entry = _walletService.CreateRandomWallet(WalletName, passphrase);
            
            // Get full details including mnemonic
            var details = _walletService.GetWalletDetails(WalletName);
            if (details != null)
            {
                Mnemonic = details.Mnemonic ?? "";
                MainnetAddress = details.MainnetAddress;
                TestnetAddress = details.TestnetAddress;
            }
            
            IsCreated = true;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            WalletNameError = $"Error creating wallet: {ex.Message}";
            HasWalletNameError = true;
        }
    }

    [RelayCommand]
    private async Task CopyMnemonic()
    {
        if (_window.Clipboard != null && !string.IsNullOrEmpty(Mnemonic))
        {
            await _window.Clipboard.SetTextAsync(Mnemonic);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        _window.Close();
    }

    [RelayCommand]
    private void Done()
    {
        _window.Close();
    }
}
