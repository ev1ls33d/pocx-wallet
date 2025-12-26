using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private string _activeWalletDisplay = "No active wallet";
    
    [ObservableProperty]
    private string _balance = "0.00000000";
    
    [ObservableProperty]
    private bool _hasActiveWallet;
    
    public WalletViewModel(IWalletService walletService, IDockerService dockerService)
    {
        _walletService = walletService;
        _dockerService = dockerService;
        
        UpdateWalletInfo();
    }
    
    private void UpdateWalletInfo()
    {
        var activeWallet = _walletService.GetActiveWallet();
        HasActiveWallet = activeWallet != null;
        
        if (activeWallet != null)
        {
            ActiveWalletDisplay = _walletService.GetActiveWalletDisplay() ?? "Unknown";
            // TODO: Get balance from node
            Balance = "0.00000000";
        }
        else
        {
            ActiveWalletDisplay = "No active wallet";
            Balance = "0.00000000";
        }
    }
    
    [RelayCommand]
    private void CreateWallet()
    {
        // TODO: Show create wallet dialog
    }
    
    [RelayCommand]
    private void ImportWallet()
    {
        // TODO: Show import wallet dialog
    }
    
    [RelayCommand]
    private void SendFunds()
    {
        // TODO: Show send funds dialog
    }
    
    [RelayCommand]
    private void ReceiveFunds()
    {
        // TODO: Show receive funds dialog
    }
    
    [RelayCommand]
    private void RefreshBalance()
    {
        UpdateWalletInfo();
    }
}
