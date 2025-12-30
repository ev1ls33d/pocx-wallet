using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocxWallet.UI.Services;
using PocxWallet.Core.Services;

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
    private string _serviceDescription = "";
    
    [ObservableProperty]
    private bool _isRunning;
    
    [ObservableProperty]
    private string _statusText = "Stopped";
    
    [ObservableProperty]
    private string _logs = "";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _statusMessage = "";
    
    [ObservableProperty]
    private bool _hasStatusMessage;
    
    [ObservableProperty]
    private ServiceDefinition? _serviceDefinition;
    
    public ObservableCollection<ServiceParameterItem> Parameters { get; } = new();
    
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
        if (ServiceDefinition != null)
        {
            ServiceDescription = ServiceDefinition.Description;
            LoadParameters();
        }
    }
    
    private void LoadParameters()
    {
        Parameters.Clear();
        if (ServiceDefinition?.Parameters != null)
        {
            // Sort parameters: active (HasUserValue) first, then by name
            var sortedParams = ServiceDefinition.Parameters
                .Where(p => !p.Hidden)
                .OrderByDescending(p => p.HasUserValue)  // Active parameters first
                .ThenBy(p => p.Name)
                .ToList();
            
            foreach (var param in sortedParams)
            {
                Parameters.Add(new ServiceParameterItem
                {
                    Name = param.Name,
                    Description = param.Description,
                    Type = param.Type,
                    Value = param.Value?.ToString() ?? param.Default?.ToString() ?? "",
                    CliFlag = param.CliFlag ?? "",
                    IsBoolean = param.Type == "bool",
                    IsActive = param.HasUserValue,  // Parameter is active if it has a user-set value
                    Category = param.Category ?? "general"
                });
            }
        }
    }
    
    private async Task UpdateStatusAsync()
    {
        try
        {
            IsRunning = await _dockerService.IsServiceRunningAsync(_serviceId);
            StatusText = IsRunning ? "Running" : "Stopped";
            
            if (IsRunning)
            {
                await RefreshLogsAsync();
            }
        }
        catch
        {
            IsRunning = false;
            StatusText = "Unknown";
        }
    }
    
    private void ShowStatus(string message)
    {
        StatusMessage = message;
        HasStatusMessage = true;
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            StatusMessage = "";
            HasStatusMessage = false;
        });
    }
    
    [RelayCommand]
    private async Task ToggleServiceAsync()
    {
        IsLoading = true;
        
        try
        {
            if (IsRunning)
            {
                ShowStatus($"Stopping {ServiceName}...");
                var stopped = await _dockerService.StopServiceAsync(_serviceId);
                if (stopped)
                {
                    ShowStatus($"? {ServiceName} stopped successfully.");
                }
                else
                {
                    ShowStatus($"Could not stop {ServiceName}.");
                }
            }
            else
            {
                ShowStatus($"Starting {ServiceName}...");
                var started = await _dockerService.StartServiceAsync(_serviceId);
                if (started)
                {
                    ShowStatus($"? {ServiceName} started successfully.");
                }
                else
                {
                    ShowStatus($"Could not start {ServiceName}. Check Docker is running.");
                }
            }
            
            await Task.Delay(1000); // Wait for container state to update
            await UpdateStatusAsync();
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
    private async Task RefreshLogsAsync()
    {
        try
        {
            if (IsRunning)
            {
                Logs = await _dockerService.GetServiceLogsAsync(_serviceId, 100);
            }
            else
            {
                Logs = "Service is not running.";
            }
        }
        catch (Exception ex)
        {
            Logs = $"Error getting logs: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task RefreshStatus()
    {
        await UpdateStatusAsync();
        ShowStatus("Status refreshed.");
    }
    
    [RelayCommand]
    private void EditParameters()
    {
        // Parameters are displayed in the UI directly
        // In a full implementation, this would open a dialog
        ShowStatus("Edit parameters in the list below and restart the service.");
    }
    
    [RelayCommand]
    private void EditSettings()
    {
        // In a full implementation, this would open a settings dialog
        ShowStatus("Settings editor will be available in a future update.");
    }
    
    [RelayCommand]
    private void ToggleParameterActive(ServiceParameterItem? param)
    {
        if (param == null || ServiceDefinition?.Parameters == null) return;
        
        var cliParam = ServiceDefinition.Parameters.FirstOrDefault(p => p.Name == param.Name);
        if (cliParam == null) return;
        
        // Toggle the active state
        param.IsActive = !param.IsActive;
        
        if (param.IsActive)
        {
            // Set the value to make it active (HasUserValue)
            if (cliParam.Type == "bool")
            {
                cliParam.Value = bool.TryParse(param.Value, out var boolVal) && boolVal;
            }
            else if (cliParam.Type == "int")
            {
                if (int.TryParse(param.Value, out var intVal))
                    cliParam.Value = intVal;
                else
                    cliParam.Value = cliParam.Default;
            }
            else
            {
                cliParam.Value = string.IsNullOrEmpty(param.Value) ? cliParam.Default : param.Value;
            }
        }
        else
        {
            // Clear the value to make it inactive
            cliParam.Value = null;
        }
        
        _configurationService.SaveServiceDefinitions();
        
        // Re-sort parameters to reflect the change
        LoadParameters();
        
        ShowStatus($"? Parameter '{param.Name}' is now {(param.IsActive ? "active" : "inactive")}.");
    }
    
    [RelayCommand]
    private void SaveParameters()
    {
        try
        {
            if (ServiceDefinition?.Parameters == null) return;
            
            foreach (var uiParam in Parameters)
            {
                var param = ServiceDefinition.Parameters.FirstOrDefault(p => p.Name == uiParam.Name);
                if (param != null)
                {
                    // Only update value if parameter is active
                    if (uiParam.IsActive)
                    {
                        if (param.Type == "bool")
                        {
                            param.Value = bool.TryParse(uiParam.Value, out var boolVal) && boolVal;
                        }
                        else if (param.Type == "int")
                        {
                            if (int.TryParse(uiParam.Value, out var intVal))
                                param.Value = intVal;
                        }
                        else
                        {
                            param.Value = uiParam.Value;
                        }
                    }
                    else
                    {
                        // Inactive parameters should have no value
                        param.Value = null;
                    }
                }
            }
            
            _configurationService.SaveServiceDefinitions();
            ShowStatus("? Parameters saved. Restart service to apply changes.");
        }
        catch (Exception ex)
        {
            ShowStatus($"Error saving parameters: {ex.Message}");
        }
    }
}

public partial class ServiceParameterItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";
    
    [ObservableProperty]
    private string _description = "";
    
    [ObservableProperty]
    private string _type = "string";
    
    [ObservableProperty]
    private string _value = "";
    
    [ObservableProperty]
    private string _cliFlag = "";
    
    [ObservableProperty]
    private bool _isBoolean;
    
    [ObservableProperty]
    private bool _isActive;
    
    [ObservableProperty]
    private string _category = "general";
}
