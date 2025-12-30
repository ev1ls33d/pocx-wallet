using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;
using System.Collections.ObjectModel;

namespace PocxWallet.UI.ViewModels;

/// <summary>
/// View model for the settings section
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IWalletService _walletService;
    private bool _isInitializing = true;
    
    [ObservableProperty]
    private bool _autoSave;
    
    [ObservableProperty]
    private bool _autoImportToNode;
    
    [ObservableProperty]
    private string? _startupWallet;
    
    [ObservableProperty]
    private string _statusMessage = "";
    
    [ObservableProperty]
    private bool _hasStatusMessage;
    
    public ObservableCollection<string> WalletNames { get; } = new();
    
    public SettingsViewModel(IConfigurationService configurationService)
        : this(configurationService, new WalletService())
    {
    }
    
    public SettingsViewModel(IConfigurationService configurationService, IWalletService walletService)
    {
        _configurationService = configurationService;
        _walletService = walletService;
        
        LoadSettings();
        _isInitializing = false;
    }
    
    private void LoadSettings()
    {
        var settings = _walletService.GetSettings();
        AutoSave = settings.AutoSave;
        AutoImportToNode = settings.AutoImportToNode;
        StartupWallet = settings.StartupWallet ?? "(none)";
        
        // Load wallet names for dropdown
        WalletNames.Clear();
        WalletNames.Add("(none)");
        foreach (var wallet in _walletService.GetAllWallets())
        {
            WalletNames.Add(wallet.Name);
        }
    }
    
    private void ShowStatus(string message)
    {
        StatusMessage = message;
        HasStatusMessage = true;
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            StatusMessage = "";
            HasStatusMessage = false;
        });
    }
    
    partial void OnAutoSaveChanged(bool value)
    {
        if (!_isInitializing) SaveSettings();
    }
    
    partial void OnAutoImportToNodeChanged(bool value)
    {
        if (!_isInitializing) SaveSettings();
    }
    
    partial void OnStartupWalletChanged(string? value)
    {
        if (!_isInitializing) SaveSettings();
    }
    
    private void SaveSettings()
    {
        var settings = new WalletSettingsModel
        {
            AutoSave = AutoSave,
            AutoImportToNode = AutoImportToNode,
            StartupWallet = StartupWallet == "(none)" ? null : StartupWallet
        };
        _walletService.UpdateSettings(settings);
        ShowStatus("? Settings saved.");
    }
    
    [RelayCommand]
    private void RefreshWallets()
    {
        _isInitializing = true;
        LoadSettings();
        _isInitializing = false;
        ShowStatus("Wallet list refreshed.");
    }
}
