using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;

namespace PocxWallet.UI.Dialogs;

public partial class WalletDetailsDialog : Window
{
    public WalletDetailsDialog()
    {
        InitializeComponent();
    }

    public WalletDetailsDialog(UIWalletDetails details) : this()
    {
        DataContext = new WalletDetailsDialogViewModel(details, this);
    }
}

public partial class WalletDetailsDialogViewModel : ObservableObject
{
    private readonly Window _window;

    [ObservableProperty]
    private string _walletName = "";

    [ObservableProperty]
    private string _walletType = "";

    [ObservableProperty]
    private bool _hasMnemonic;

    [ObservableProperty]
    private string _mnemonic = "";

    [ObservableProperty]
    private string _mainnetAddress = "";

    [ObservableProperty]
    private string _testnetAddress = "";

    [ObservableProperty]
    private string _wifMainnet = "";

    [ObservableProperty]
    private string _wifTestnet = "";

    [ObservableProperty]
    private string _descriptorMainnet = "";

    [ObservableProperty]
    private string _descriptorTestnet = "";

    [ObservableProperty]
    private string _created = "";

    public WalletDetailsDialogViewModel(UIWalletDetails details, Window window)
    {
        _window = window;

        WalletName = details.Name;
        WalletType = details.IsSingleKeyWallet ? "Single-Key Wallet" : "HD Wallet (BIP84)";
        HasMnemonic = !details.IsSingleKeyWallet && !string.IsNullOrEmpty(details.Mnemonic);
        Mnemonic = details.Mnemonic ?? "";
        MainnetAddress = details.MainnetAddress;
        TestnetAddress = details.TestnetAddress;
        WifMainnet = details.WifMainnet ?? "";
        WifTestnet = details.WifTestnet ?? "";
        DescriptorMainnet = details.DescriptorMainnet ?? "";
        DescriptorTestnet = details.DescriptorTestnet ?? "";
        
        // Format created date
        if (DateTime.TryParse(details.Created, out var createdDate))
        {
            Created = createdDate.ToLocalTime().ToString("g");
        }
        else
        {
            Created = details.Created;
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
    private async Task CopyMainnetAddress()
    {
        if (_window.Clipboard != null && !string.IsNullOrEmpty(MainnetAddress))
        {
            await _window.Clipboard.SetTextAsync(MainnetAddress);
        }
    }

    [RelayCommand]
    private async Task CopyTestnetAddress()
    {
        if (_window.Clipboard != null && !string.IsNullOrEmpty(TestnetAddress))
        {
            await _window.Clipboard.SetTextAsync(TestnetAddress);
        }
    }

    [RelayCommand]
    private void Close()
    {
        _window.Close();
    }
}
