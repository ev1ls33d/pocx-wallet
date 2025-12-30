using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;

namespace PocxWallet.UI.Dialogs;

public partial class ImportMnemonicDialog : Window
{
    public ImportMnemonicDialog()
    {
        InitializeComponent();
    }

    public ImportMnemonicDialog(IWalletService walletService) : this()
    {
        DataContext = new ImportMnemonicDialogViewModel(walletService, this);
    }
}

public partial class ImportMnemonicDialogViewModel : ObservableObject
{
    private readonly IWalletService _walletService;
    private readonly Window _window;

    [ObservableProperty]
    private string _walletName = "restored";

    [ObservableProperty]
    private string _mnemonicPhrase = "";

    [ObservableProperty]
    private string _passphrase = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isImported;

    [ObservableProperty]
    private string _mainnetAddress = "";

    [ObservableProperty]
    private string _testnetAddress = "";

    public bool? DialogResult { get; private set; }

    public ImportMnemonicDialogViewModel(IWalletService walletService, Window window)
    {
        _walletService = walletService;
        _window = window;
    }

    [RelayCommand]
    private void Import()
    {
        HasError = false;

        // Validate wallet name
        if (string.IsNullOrWhiteSpace(WalletName))
        {
            ErrorMessage = "Wallet name is required.";
            HasError = true;
            return;
        }

        if (_walletService.WalletNameExists(WalletName))
        {
            ErrorMessage = $"Wallet '{WalletName}' already exists.";
            HasError = true;
            return;
        }

        // Validate mnemonic
        if (string.IsNullOrWhiteSpace(MnemonicPhrase))
        {
            ErrorMessage = "Mnemonic phrase is required.";
            HasError = true;
            return;
        }

        if (!_walletService.ValidateMnemonic(MnemonicPhrase.Trim()))
        {
            ErrorMessage = "Invalid mnemonic phrase. Please check your words.";
            HasError = true;
            return;
        }

        try
        {
            var passphrase = string.IsNullOrEmpty(Passphrase) ? null : Passphrase;
            var entry = _walletService.ImportFromMnemonic(WalletName, MnemonicPhrase.Trim(), passphrase);
            
            MainnetAddress = entry.MainnetAddress;
            TestnetAddress = entry.TestnetAddress;
            
            IsImported = true;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error importing wallet: {ex.Message}";
            HasError = true;
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
