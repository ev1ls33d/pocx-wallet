using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;
using PocxWallet.UI.Models;

namespace PocxWallet.UI.ViewModels;

/// <summary>
/// View model for service management (Node, Plotter, Miner, Aggregator, Electrs)
/// </summary>
public partial class ServiceViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly IConfigurationService _configurationService;
    private readonly string _serviceId;
    
    [ObservableProperty]
    private string _serviceName;
    
    [ObservableProperty]
    private bool _isRunning;
    
    [ObservableProperty]
    private string _statusText = "Stopped";
    
    [ObservableProperty]
    private string _logs = "";
    
    [ObservableProperty]
    private ServiceDefinition? _serviceDefinition;
    
    public ServiceViewModel(
        IDockerService dockerService,
        IConfigurationService configurationService,
        string serviceId,
        string serviceName)
    {
        _dockerService = dockerService;
        _configurationService = configurationService;
        _serviceId = serviceId;
        _serviceName = serviceName;
        
        LoadServiceDefinition();
        _ = UpdateStatusAsync();
    }
    
    private void LoadServiceDefinition()
    {
        ServiceDefinition = _configurationService.GetServiceDefinition(_serviceId);
    }
    
    private async Task UpdateStatusAsync()
    {
        IsRunning = await _dockerService.IsServiceRunningAsync(_serviceId);
        StatusText = IsRunning ? "Running" : "Stopped";
        
        if (IsRunning)
        {
            await RefreshLogsAsync();
        }
    }
    
    [RelayCommand]
    private async Task ToggleServiceAsync()
    {
        if (IsRunning)
        {
            await _dockerService.StopServiceAsync(_serviceId);
        }
        else
        {
            await _dockerService.StartServiceAsync(_serviceId);
        }
        
        await UpdateStatusAsync();
    }
    
    [RelayCommand]
    private async Task RefreshLogsAsync()
    {
        if (IsRunning)
        {
            Logs = await _dockerService.GetServiceLogsAsync(_serviceId, 100);
        }
    }
    
    [RelayCommand]
    private void EditParameters()
    {
        // TODO: Show parameters dialog
    }
    
    [RelayCommand]
    private void EditSettings()
    {
        // TODO: Show settings dialog
    }
}
