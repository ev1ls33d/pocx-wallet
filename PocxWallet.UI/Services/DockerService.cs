using PocxWallet.Core.Services;

namespace PocxWallet.UI.Services;

/// <summary>
/// Implementation of IDockerService using Core DockerManager
/// </summary>
public class DockerService : IDockerService
{
    private readonly DockerManager _dockerManager;
    private readonly ServiceConfigurationLoader _configLoader;
    private ServiceConfiguration? _serviceConfig;

    public DockerService()
    {
        _dockerManager = new DockerManager();
        _configLoader = new ServiceConfigurationLoader();
    }

    private ServiceConfiguration GetServiceConfig()
    {
        return _serviceConfig ??= _configLoader.LoadServices() ?? new ServiceConfiguration();
    }

    public async Task<bool> IsServiceRunningAsync(string serviceId)
    {
        var containerName = GetContainerName(serviceId);
        var status = await _dockerManager.GetContainerStatusAsync(containerName);
        return status.Equals("running", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> StartServiceAsync(string serviceId)
    {
        var config = GetServiceConfig();
        var service = config.Services?.FirstOrDefault(s => s.Id == serviceId);
        if (service == null)
            return false;

        try
        {
            // Use ServiceDefinition helper methods for getting Docker image info
            var containerName = service.GetContainerName();
            var repository = service.GetDockerRepository();
            var image = service.GetDockerImageName();
            var tag = service.GetDockerTag();
            var network = service.NetworkOverride ?? config.Defaults?.DockerNetwork ?? "pocx";
            
            // Build command using Core helper
            var command = ServiceConfigurationHelper.BuildCommand(service);
            
            // Build environment variables using Core helper
            var envVars = ServiceConfigurationHelper.BuildEnvironmentVariables(service);
            
            // Build volume mounts using Core helper
            var volumes = ServiceConfigurationHelper.BuildVolumeMappings(service);
            
            // Ensure directories exist for volumes
            if (service.Volumes != null)
            {
                foreach (var volume in service.Volumes)
                {
                    var hostPath = volume.HostPathOverride ?? volume.HostPathDefault;
                    if (!string.IsNullOrEmpty(hostPath) && !volume.IsFile)
                    {
                        var fullPath = Path.GetFullPath(hostPath);
                        Directory.CreateDirectory(fullPath);
                        // Update the mapping with the full path
                        if (volumes.ContainsKey(hostPath))
                        {
                            var containerPath = volumes[hostPath];
                            volumes.Remove(hostPath);
                            volumes[fullPath] = containerPath;
                        }
                    }
                }
            }
            
            // Build port mappings using Core helper
            var ports = ServiceConfigurationHelper.BuildPortMappings(service);
            
            // Get read-only volumes
            var readOnlyVolumes = ServiceConfigurationHelper.GetReadOnlyVolumes(service);
            
            // Ensure network exists
            await _dockerManager.EnsureNetworkExistsAsync(network);
            
            // Start container
            return await _dockerManager.StartContainerAsync(
                containerName,
                image,
                repository,
                tag,
                envVars.Count > 0 ? envVars : null,
                volumes.Count > 0 ? volumes : null,
                ports.Count > 0 ? ports : null,
                command,
                network,
                readOnlyVolumes.Count > 0 ? readOnlyVolumes : null,
                false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error starting service {serviceId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopServiceAsync(string serviceId)
    {
        var containerName = GetContainerName(serviceId);
        return await _dockerManager.StopContainerAsync(containerName);
    }

    public async Task<string> GetServiceLogsAsync(string serviceId, int tailLines = 100)
    {
        var containerName = GetContainerName(serviceId);
        return await _dockerManager.GetContainerLogsAsync(containerName, tailLines);
    }

    public async Task<(int exitCode, string output)> ExecuteInContainerAsync(string containerName, string command)
    {
        return await _dockerManager.ExecInContainerAsync(containerName, command);
    }

    public string GetContainerName(string serviceId)
    {
        var config = GetServiceConfig();
        var service = config.Services?.FirstOrDefault(s => s.Id == serviceId);
        if (service != null)
        {
            return service.GetContainerName();
        }
        
        // Fallback mapping for when service is not found
        return serviceId switch
        {
            "bitcoin-node" or "node" => "pocx-node",
            "plotter" => "pocx-plotter",
            "miner" => "pocx-miner",
            "aggregator" => "pocx-aggregator",
            "electrs" => "pocx-electrs",
            _ => $"pocx-{serviceId}"
        };
    }

    public bool IsNodeTestnet()
    {
        var config = GetServiceConfig();
        var nodeService = config.Services?.FirstOrDefault(s => 
            s.Id?.Equals("node", StringComparison.OrdinalIgnoreCase) == true ||
            s.Id?.Equals("bitcoin-node", StringComparison.OrdinalIgnoreCase) == true);
        
        if (nodeService?.Parameters == null)
            return true; // Default to testnet for safety
        
        var testnetParam = nodeService.Parameters.FirstOrDefault(p => 
            p.Name?.Equals("testnet", StringComparison.OrdinalIgnoreCase) == true);
        
        if (testnetParam != null)
        {
            if (testnetParam.Value != null && bool.TryParse(testnetParam.Value.ToString(), out var isTestnet))
                return isTestnet;
            if (testnetParam.Default != null && bool.TryParse(testnetParam.Default.ToString(), out var defaultTestnet))
                return defaultTestnet;
        }
        
        return true; // Default to testnet for safety
    }

    public async Task<string> GetWalletBalanceAsync(string walletName, bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        var command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} getbalance";
        var (exitCode, output) = await ExecuteInContainerAsync(containerName, command);
        return output;
    }

    public async Task<string> GetBlockchainInfoAsync(bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        var command = $"bitcoin-cli {networkFlag}getblockchaininfo";
        var (exitCode, output) = await ExecuteInContainerAsync(containerName, command);
        return output;
    }

    public async Task<string> GetWalletInfoAsync(string walletName, bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        var command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} getwalletinfo";
        var (exitCode, output) = await ExecuteInContainerAsync(containerName, command);
        return output;
    }

    public async Task<string> GetTransactionHistoryAsync(string walletName, int count = 10, bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        var command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} listtransactions \"*\" {count}";
        var (exitCode, output) = await ExecuteInContainerAsync(containerName, command);
        return output;
    }

    public async Task<string> GetListUnspentAsync(string walletName, bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        var command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} listunspent";
        var (exitCode, output) = await ExecuteInContainerAsync(containerName, command);
        return output;
    }

    public async Task<(bool success, string result)> SendToAddressAsync(string walletName, string address, decimal amount, bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        var command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} sendtoaddress \"{address}\" {amount}";
        var (exitCode, output) = await ExecuteInContainerAsync(containerName, command);
        return (exitCode == 0, output);
    }

    public async Task<(bool success, string result)> ImportWalletToNodeAsync(string walletName, string descriptor, bool isTestnet = true)
    {
        var containerName = GetContainerName("bitcoin-node");
        var networkFlag = isTestnet ? "-testnet " : "";
        
        // Check if wallet exists
        var listDirCmd = $"bitcoin-cli {networkFlag}listwalletdir";
        var (_, listOutput) = await ExecuteInContainerAsync(containerName, listDirCmd);
        
        bool walletExists = listOutput.Contains($"\"name\": \"{walletName}\"") || 
                           listOutput.Contains($"\"name\":\"{walletName}\"");
        
        if (walletExists)
        {
            // Load wallet
            var loadCmd = $"bitcoin-cli {networkFlag}loadwallet \"{walletName}\" true";
            await ExecuteInContainerAsync(containerName, loadCmd);
        }
        else
        {
            // Create wallet
            var createCmd = $"bitcoin-cli {networkFlag}createwallet \"{walletName}\" false";
            var (createExitCode, createOutput) = await ExecuteInContainerAsync(containerName, createCmd);
            if (createExitCode != 0 && !createOutput.Contains("already exists"))
            {
                return (false, createOutput);
            }
        }
        
        // Import descriptor
        var escapedDescriptor = descriptor.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var importJson = $"'[{{\"desc\": \"{escapedDescriptor}\", \"timestamp\": \"now\"}}]'";
        var importCmd = $"bitcoin-cli {networkFlag}-rpcwallet=\"{walletName}\" importdescriptors {importJson}";
        var (importExitCode, importOutput) = await ExecuteInContainerAsync(containerName, importCmd);
        
        var success = importExitCode == 0 && 
                     (importOutput.Contains("\"success\": true") || importOutput.Contains("\"success\":true"));
        
        return (success || importOutput.Contains("already"), importOutput);
    }
}
