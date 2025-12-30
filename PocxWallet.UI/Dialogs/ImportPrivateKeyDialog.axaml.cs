using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;

namespace PocxWallet.UI.Dialogs;

public partial class ImportPrivateKeyDialog : Window
{
    public ImportPrivateKeyDialog()
    {
        InitializeComponent();
    }

    public ImportPrivateKeyDialog(IWalletService walletService) : this()
    {
        DataContext = new ImportPrivateKeyDialogViewModel(walletService, this);
    }
}

public partial class ImportPrivateKeyDialogViewModel : ObservableObject
{
    private readonly IWalletService _walletService;
    private readonly Window _window;

    [ObservableProperty]
    private string _walletName = "imported";

    [ObservableProperty]
    private string _privateKeyOrWif = "";

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

    public ImportPrivateKeyDialogViewModel(IWalletService walletService, Window window)
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

        // Validate private key
        if (string.IsNullOrWhiteSpace(PrivateKeyOrWif))
        {
            ErrorMessage = "Private key or WIF is required.";
            HasError = true;
            return;
        }

        if (!_walletService.ValidatePrivateKey(PrivateKeyOrWif.Trim()))
        {
            ErrorMessage = "Invalid private key or WIF format.";
            HasError = true;
            return;
        }

        try
        {
            var entry = _walletService.ImportFromPrivateKey(WalletName, PrivateKeyOrWif.Trim());
            
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
