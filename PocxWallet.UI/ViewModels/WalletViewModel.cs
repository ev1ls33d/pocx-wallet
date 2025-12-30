using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Dialogs;
using PocxWallet.UI.Models;
using PocxWallet.UI.Services;

namespace PocxWallet.UI.ViewModels;

/// <summary>
/// View model for the wallet section
/// </summary>
public partial class WalletViewModel : ViewModelBase
{
    private readonly IWalletService _walletService;
    private readonly IDockerService _dockerService;
    
    [ObservableProperty]
    private string _activeWalletName = "No active wallet";
    
    [ObservableProperty]
    private string _activeWalletAddress = "";
    
    [ObservableProperty]
    private string _balance = "0.00000000";
    
    [ObservableProperty]
    private bool _hasActiveWallet;
    
    [ObservableProperty]
    private bool _isNodeRunning;
    
    [ObservableProperty]
    private string _nodeStatus = "Unknown";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _statusMessage = "";
    
    [ObservableProperty]
    private bool _hasStatusMessage;
    
    [ObservableProperty]
    private bool _isTestnet = true;
    
    public ObservableCollection<UIWalletEntry> Wallets { get; } = new();
    
    public WalletViewModel(IWalletService walletService, IDockerService dockerService)
    {
        _walletService = walletService;
        _dockerService = dockerService;
        
        IsTestnet = _dockerService.IsNodeTestnet();
        
        UpdateWalletInfo();
        _ = CheckNodeStatusAsync();
    }
    
    private void UpdateWalletInfo()
    {
        var activeWallet = _walletService.GetActiveWallet();
        HasActiveWallet = activeWallet != null;
        
        if (activeWallet != null)
        {
            ActiveWalletName = activeWallet.Name;
            ActiveWalletAddress = IsTestnet ? activeWallet.TestnetAddress : activeWallet.MainnetAddress;
        }
        else
        {
            ActiveWalletName = "No active wallet";
            ActiveWalletAddress = "";
            Balance = "0.00000000";
        }
        
        // Update wallet list
        Wallets.Clear();
        foreach (var wallet in _walletService.GetAllWallets())
        {
            Wallets.Add(wallet);
        }
    }
    
    private async Task CheckNodeStatusAsync()
    {
        try
        {
            IsNodeRunning = await _dockerService.IsServiceRunningAsync("bitcoin-node");
            NodeStatus = IsNodeRunning ? "Running" : "Stopped";
        }
        catch
        {
            IsNodeRunning = false;
            NodeStatus = "Unknown";
        }
    }
    
    private void ShowStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        HasStatusMessage = true;
        
        // Auto-hide after a delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            StatusMessage = "";
            HasStatusMessage = false;
        });
    }
    
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
    
    [RelayCommand]
    private async Task CreateWallet()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        var dialog = new CreateWalletDialog(_walletService);
        await dialog.ShowDialog(mainWindow);
        
        UpdateWalletInfo();
    }
    
    [RelayCommand]
    private async Task ImportFromMnemonic()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        var dialog = new ImportMnemonicDialog(_walletService);
        await dialog.ShowDialog(mainWindow);
        
        UpdateWalletInfo();
    }
    
    [RelayCommand]
    private async Task ImportFromPrivateKey()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        var dialog = new ImportPrivateKeyDialog(_walletService);
        await dialog.ShowDialog(mainWindow);
        
        UpdateWalletInfo();
    }
    
    [RelayCommand]
    private async Task SelectWallet()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        if (Wallets.Count == 0)
        {
            ShowStatus("No wallets available. Create a wallet first.");
            return;
        }
        
        var dialog = new SelectWalletDialog(_walletService);
        await dialog.ShowDialog(mainWindow);
        
        if (dialog.DialogResult == true && dialog.SelectedWalletName != null)
        {
            ShowStatus($"Switched to wallet: {dialog.SelectedWalletName}");
        }
        
        UpdateWalletInfo();
    }
    
    [RelayCommand]
    private async Task RemoveWallet()
    {
        if (!HasActiveWallet) return;
        
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        var dialog = new ConfirmDialog(
            "Remove Wallet",
            $"Are you sure you want to remove wallet '{ActiveWalletName}'?\n\nThis action cannot be undone.",
            "Remove",
            "Cancel");
        
        await dialog.ShowDialog(mainWindow);
        
        if (dialog.DialogResult == true)
        {
            var name = ActiveWalletName;
            if (_walletService.RemoveWallet(name))
            {
                ShowStatus($"Wallet '{name}' removed.");
                UpdateWalletInfo();
            }
        }
    }
    
    [RelayCommand]
    private async Task ViewWalletDetails()
    {
        if (!HasActiveWallet) return;
        
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        var details = _walletService.GetWalletDetails(ActiveWalletName);
        if (details == null)
        {
            ShowStatus("Could not load wallet details.");
            return;
        }
        
        var dialog = new WalletDetailsDialog(details);
        await dialog.ShowDialog(mainWindow);
    }
    
    [RelayCommand]
    private async Task SendFunds()
    {
        if (!HasActiveWallet) return;
        
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        if (!IsNodeRunning)
        {
            ShowStatus("Node is not running. Start the node first.");
            return;
        }
        
        var dialog = new SendFundsDialog(_dockerService, ActiveWalletName, IsTestnet);
        await dialog.ShowDialog(mainWindow);
    }
    
    [RelayCommand]
    private async Task ReceiveFunds()
    {
        if (!HasActiveWallet) return;
        
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        // Show wallet details with addresses
        var details = _walletService.GetWalletDetails(ActiveWalletName);
        if (details == null) return;
        
        var dialog = new WalletDetailsDialog(details);
        await dialog.ShowDialog(mainWindow);
    }
    
    [RelayCommand]
    private async Task RefreshBalance()
    {
        if (!HasActiveWallet)
        {
            Balance = "0.00000000";
            return;
        }
        
        IsLoading = true;
        StatusMessage = "Refreshing...";
        HasStatusMessage = true;
        
        try
        {
            await CheckNodeStatusAsync();
            
            if (!IsNodeRunning)
            {
                ShowStatus("Node is not running. Start the node to check balance.");
                return;
            }
            
            var balanceResult = await _dockerService.GetWalletBalanceAsync(ActiveWalletName, IsTestnet);
            
            if (decimal.TryParse(balanceResult.Trim(), out var balanceValue))
            {
                Balance = balanceValue.ToString("F8");
                ShowStatus("Balance updated.");
            }
            else if (balanceResult.Contains("wallet"))
            {
                ShowStatus("Wallet not imported to node. Import wallet first.");
            }
            else
            {
                ShowStatus($"Could not get balance: {balanceResult}");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task ImportToNode()
    {
        if (!HasActiveWallet) return;
        
        IsLoading = true;
        ShowStatus("Importing wallet to node...");
        
        try
        {
            await CheckNodeStatusAsync();
            
            if (!IsNodeRunning)
            {
                ShowStatus("Node is not running. Start the node first.");
                return;
            }
            
            var details = _walletService.GetWalletDetails(ActiveWalletName);
            if (details == null)
            {
                ShowStatus("Could not load wallet details.");
                return;
            }
            
            var descriptor = IsTestnet ? details.DescriptorTestnet : details.DescriptorMainnet;
            if (string.IsNullOrEmpty(descriptor))
            {
                ShowStatus("Wallet descriptor not available.");
                return;
            }
            
            var (success, result) = await _dockerService.ImportWalletToNodeAsync(ActiveWalletName, descriptor, IsTestnet);
            
            if (success)
            {
                ShowStatus("? Wallet imported to node successfully.");
            }
            else
            {
                ShowStatus($"Import failed: {result}");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task GetBlockchainInfo()
    {
        IsLoading = true;
        ShowStatus("Fetching blockchain info...");
        
        try
        {
            if (!IsNodeRunning)
            {
                ShowStatus("Node is not running.");
                return;
            }
            
            var info = await _dockerService.GetBlockchainInfoAsync(IsTestnet);
            // For now, just show a brief status. In future, show a dialog with full info.
            if (info.Contains("blocks"))
            {
                ShowStatus("? Blockchain info retrieved. See logs for details.");
            }
            else
            {
                ShowStatus(info);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task GetTransactionHistory()
    {
        if (!HasActiveWallet) return;
        
        IsLoading = true;
        ShowStatus("Fetching transaction history...");
        
        try
        {
            if (!IsNodeRunning)
            {
                ShowStatus("Node is not running.");
                return;
            }
            
            var history = await _dockerService.GetTransactionHistoryAsync(ActiveWalletName, 10, IsTestnet);
            
            if (history.Contains("[]"))
            {
                ShowStatus("No transactions found.");
            }
            else if (history.Contains("txid"))
            {
                ShowStatus("? Transaction history retrieved.");
            }
            else
            {
                ShowStatus(history);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task CopyAddress()
    {
        if (string.IsNullOrEmpty(ActiveWalletAddress)) return;
        
        var mainWindow = GetMainWindow();
        if (mainWindow?.Clipboard != null)
        {
            await mainWindow.Clipboard.SetTextAsync(ActiveWalletAddress);
            ShowStatus("Address copied to clipboard.");
        }
    }
}
