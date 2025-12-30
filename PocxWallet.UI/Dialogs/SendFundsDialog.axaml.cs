using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;
using System.Globalization;

namespace PocxWallet.UI.Dialogs;

public partial class SendFundsDialog : Window
{
    public SendFundsDialog()
    {
        InitializeComponent();
    }

    public SendFundsDialog(IDockerService dockerService, string walletName, bool isTestnet) : this()
    {
        DataContext = new SendFundsDialogViewModel(dockerService, walletName, isTestnet, this);
    }
}

public partial class SendFundsDialogViewModel : ObservableObject
{
    private readonly IDockerService _dockerService;
    private readonly string _walletName;
    private readonly bool _isTestnet;
    private readonly Window _window;

    [ObservableProperty]
    private string _walletInfo = "";

    [ObservableProperty]
    private string _destinationAddress = "";

    [ObservableProperty]
    private string _amount = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _successMessage = "";

    [ObservableProperty]
    private bool _isSent;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string _transactionId = "";

    public SendFundsDialogViewModel(IDockerService dockerService, string walletName, bool isTestnet, Window window)
    {
        _dockerService = dockerService;
        _walletName = walletName;
        _isTestnet = isTestnet;
        _window = window;
        
        WalletInfo = $"From: {walletName} ({(isTestnet ? "testnet" : "mainnet")})";
    }

    [RelayCommand]
    private async Task Send()
    {
        HasError = false;
        IsSending = true;

        try
        {
            // Validate address
            if (string.IsNullOrWhiteSpace(DestinationAddress))
            {
                ErrorMessage = "Destination address is required.";
                HasError = true;
                return;
            }

            // Validate amount
            if (string.IsNullOrWhiteSpace(Amount))
            {
                ErrorMessage = "Amount is required.";
                HasError = true;
                return;
            }

            if (!decimal.TryParse(Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amountValue) || amountValue <= 0)
            {
                ErrorMessage = "Invalid amount. Please enter a positive number.";
                HasError = true;
                return;
            }

            // Check if node is running
            var isRunning = await _dockerService.IsServiceRunningAsync("bitcoin-node");
            if (!isRunning)
            {
                ErrorMessage = "Bitcoin node is not running. Please start the node first.";
                HasError = true;
                return;
            }

            // Send the transaction
            var (success, result) = await _dockerService.SendToAddressAsync(_walletName, DestinationAddress.Trim(), amountValue, _isTestnet);
            
            if (success)
            {
                TransactionId = result.Trim();
                SuccessMessage = "? Transaction sent successfully!";
                IsSent = true;
            }
            else
            {
                ErrorMessage = $"Transaction failed: {result}";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.Close();
    }

    [RelayCommand]
    private void Close()
    {
        _window.Close();
    }
}
