using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;

namespace PocxWallet.UI.ViewModels;

/// <summary>
/// View model for the main window
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IWalletService _walletService;
    private readonly IDockerService _dockerService;
    private readonly IConfigurationService _configurationService;
    
    [ObservableProperty]
    private ViewModelBase? _currentView;
    
    [ObservableProperty]
    private string _title = "PoCX Wallet";
    
    [ObservableProperty]
    private string _version = "v0.7.3";
    
    public ObservableCollection<NavigationItem> NavigationItems { get; }
    
    public MainWindowViewModel(
        IWalletService walletService,
        IDockerService dockerService,
        IConfigurationService configurationService)
    {
        _walletService = walletService;
        _dockerService = dockerService;
        _configurationService = configurationService;
        
        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new() { Icon = "📁", Label = "Wallet", ViewType = NavigationViewType.Wallet },
            new() { Icon = "🖥️", Label = "Node", ViewType = NavigationViewType.Node },
            new() { Icon = "📊", Label = "Plot", ViewType = NavigationViewType.Plot },
            new() { Icon = "⛏️", Label = "Mine", ViewType = NavigationViewType.Mine },
            new() { Icon = "🔗", Label = "Aggregator", ViewType = NavigationViewType.Aggregator },
            new() { Icon = "⚡", Label = "Electrs", ViewType = NavigationViewType.Electrs },
            new() { Icon = "⚙️", Label = "Settings", ViewType = NavigationViewType.Settings },
            new() { Icon = "ℹ️", Label = "About", ViewType = NavigationViewType.About }
        };
        
        // Load wallet on startup
        _walletService.Load();
        
        // Set default view to Wallet
        NavigateToWallet();
    }
    
    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        CurrentView = item.ViewType switch
        {
            NavigationViewType.Wallet => new WalletViewModel(_walletService, _dockerService),
            NavigationViewType.Node => new ServiceViewModel(_dockerService, _configurationService, "bitcoin-node", "Node"),
            NavigationViewType.Plot => new ServiceViewModel(_dockerService, _configurationService, "plotter", "Plotter"),
            NavigationViewType.Mine => new ServiceViewModel(_dockerService, _configurationService, "miner", "Miner"),
            NavigationViewType.Aggregator => new ServiceViewModel(_dockerService, _configurationService, "aggregator", "Aggregator"),
            NavigationViewType.Electrs => new ServiceViewModel(_dockerService, _configurationService, "electrs", "Electrs"),
            NavigationViewType.Settings => new SettingsViewModel(_configurationService),
            NavigationViewType.About => new AboutViewModel(),
            _ => CurrentView
        };
    }
    
    private void NavigateToWallet()
    {
        CurrentView = new WalletViewModel(_walletService, _dockerService);
    }
}

public class NavigationItem
{
    public string Icon { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public NavigationViewType ViewType { get; set; }
}

public enum NavigationViewType
{
    Wallet,
    Node,
    Plot,
    Mine,
    Aggregator,
    Electrs,
    Settings,
    About
}
