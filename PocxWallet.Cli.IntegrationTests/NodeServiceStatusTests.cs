using Xunit;
using Xunit.Abstractions;
using PocxWallet.Cli.Services;
using PocxWallet.Cli.Configuration;
using Microsoft.Extensions.Configuration;

namespace PocxWallet.Cli.IntegrationTests;

/// <summary>
/// Integration tests for Node service status management.
/// These tests require Docker to be installed and running.
/// </summary>
public class NodeServiceStatusTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private DockerServiceManager? _dockerManager;
    private AppSettings? _settings;
    private readonly string _testContainerName = "pocx-node-test";
    
    public NodeServiceStatusTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public async Task InitializeAsync()
    {
        _output.WriteLine("Initializing test environment...");
        
        // Load settings
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        
        _settings = new AppSettings();
        config.Bind(_settings);
        
        // Override container name for testing
        _settings.BitcoinContainerName = _testContainerName;
        
        _dockerManager = new DockerServiceManager();
        
        // Check if Docker is available
        var dockerAvailable = await _dockerManager.IsDockerAvailableAsync();
        if (!dockerAvailable)
        {
            throw new InvalidOperationException("Docker is not available. Please install Docker to run integration tests.");
        }
        
        // Clean up any existing test container
        await CleanupTestContainerAsync();
        
        _output.WriteLine("Test environment initialized.");
    }
    
    public async Task DisposeAsync()
    {
        _output.WriteLine("Cleaning up test environment...");
        await CleanupTestContainerAsync();
        _output.WriteLine("Test environment cleaned up.");
    }
    
    private async Task CleanupTestContainerAsync()
    {
        if (_dockerManager == null) return;
        
        try
        {
            var status = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            if (status != "not found")
            {
                _output.WriteLine($"Stopping test container: {_testContainerName}");
                await _dockerManager.StopContainerAsync(_testContainerName);
                await _dockerManager.RemoveContainerAsync(_testContainerName);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }
    
    [Fact]
    public async Task StartNode_ShouldUpdateStatusToRunning()
    {
        // Arrange
        _output.WriteLine("TEST: Starting node container...");
        Assert.NotNull(_dockerManager);
        Assert.NotNull(_settings);
        
        // Verify initial status is not running
        var initialStatus = await _dockerManager.GetContainerStatusAsync(_testContainerName);
        _output.WriteLine($"Initial container status: {initialStatus}");
        Assert.Equal("not found", initialStatus);
        
        // Create test data directory
        var testDataDir = Path.Combine(Path.GetTempPath(), $"bitcoin-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDataDir);
        _output.WriteLine($"Test data directory: {testDataDir}");
        
        try
        {
            // Act - Start the container using alpine as test image (lightweight and available)
            var volumeMounts = new Dictionary<string, string>
            {
                { testDataDir, "/data" }
            };
            
            var envVars = new Dictionary<string, string>();
            
            _output.WriteLine($"Starting container with test image: alpine:latest");
            
            var success = await _dockerManager.StartContainerAsync(
                _testContainerName,
                "alpine",
                "docker.io/library",
                "latest",
                environmentVars: envVars,
                volumeMounts: volumeMounts,
                portMappings: null,
                command: "sleep 300", // Keep container running for test
                network: null
            );
            
            _output.WriteLine($"Start container returned: {success}");
            
            // Assert - Verify the container started successfully
            Assert.True(success, "Container should start successfully");
            
            // Wait a moment for status to stabilize
            await Task.Delay(500);
            
            // Verify status is now "running"
            var statusAfterStart = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            _output.WriteLine($"Container status after start: {statusAfterStart}");
            Assert.Equal("running", statusAfterStart);
            
            // Additional verification - check if container appears in ps
            var containers = await _dockerManager.ListContainersAsync();
            var containerNames = containers.Select(c => c.Name).ToList();
            _output.WriteLine($"Running containers: {string.Join(", ", containerNames)}");
            Assert.Contains(_testContainerName, containerNames);
            
            _output.WriteLine("TEST PASSED: Node started and status updated correctly");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDataDir))
            {
                try
                {
                    Directory.Delete(testDataDir, true);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Warning: Could not delete test data directory: {ex.Message}");
                }
            }
        }
    }
    
    [Fact]
    public async Task StopNode_ShouldUpdateStatusToStopped()
    {
        // Arrange
        _output.WriteLine("TEST: Starting and stopping node container...");
        Assert.NotNull(_dockerManager);
        Assert.NotNull(_settings);
        
        var testDataDir = Path.Combine(Path.GetTempPath(), $"bitcoin-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDataDir);
        
        try
        {
            // Start container first using alpine test image
            var volumeMounts = new Dictionary<string, string>
            {
                { testDataDir, "/data" }
            };
            
            _output.WriteLine("Starting container...");
            var startSuccess = await _dockerManager.StartContainerAsync(
                _testContainerName,
                "alpine",
                "docker.io/library",
                "latest",
                volumeMounts: volumeMounts,
                command: "sleep 300",
                network: null
            );
            
            Assert.True(startSuccess, "Container should start successfully");
            
            var statusAfterStart = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            _output.WriteLine($"Status after start: {statusAfterStart}");
            Assert.Equal("running", statusAfterStart);
            
            // Act - Stop the container
            _output.WriteLine("Stopping container...");
            var stopSuccess = await _dockerManager.StopContainerAsync(_testContainerName);
            Assert.True(stopSuccess, "Container should stop successfully");
            
            // Wait for status to update
            await Task.Delay(500);
            
            // Assert - Verify status is no longer "running"
            var statusAfterStop = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            _output.WriteLine($"Status after stop: {statusAfterStop}");
            Assert.NotEqual("running", statusAfterStop);
            Assert.True(statusAfterStop == "exited" || statusAfterStop == "stopped", 
                $"Expected 'exited' or 'stopped', but got '{statusAfterStop}'");
            
            _output.WriteLine("TEST PASSED: Node stopped and status updated correctly");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDataDir))
            {
                try
                {
                    Directory.Delete(testDataDir, true);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Warning: Could not delete test data directory: {ex.Message}");
                }
            }
        }
    }
    
    [Fact]
    public async Task RestartNode_ShouldProperlyUpdateStatus()
    {
        // Arrange
        _output.WriteLine("TEST: Testing node restart and status updates...");
        Assert.NotNull(_dockerManager);
        Assert.NotNull(_settings);
        
        var testDataDir = Path.Combine(Path.GetTempPath(), $"bitcoin-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDataDir);
        
        try
        {
            var volumeMounts = new Dictionary<string, string>
            {
                { testDataDir, "/data" }
            };
            
            // Start container using alpine test image
            _output.WriteLine("Starting container (first time)...");
            var firstStart = await _dockerManager.StartContainerAsync(
                _testContainerName,
                "alpine",
                "docker.io/library",
                "latest",
                volumeMounts: volumeMounts,
                command: "sleep 300",
                network: null
            );
            
            Assert.True(firstStart, "First start should succeed");
            var statusFirst = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            _output.WriteLine($"Status after first start: {statusFirst}");
            Assert.Equal("running", statusFirst);
            
            // Stop container
            _output.WriteLine("Stopping container...");
            await _dockerManager.StopContainerAsync(_testContainerName);
            await Task.Delay(500);
            
            var statusAfterStop = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            _output.WriteLine($"Status after stop: {statusAfterStop}");
            Assert.NotEqual("running", statusAfterStop);
            
            // Start container again
            _output.WriteLine("Starting container (second time)...");
            var secondStart = await _dockerManager.StartContainerAsync(
                _testContainerName,
                "alpine",
                "docker.io/library",
                "latest",
                volumeMounts: volumeMounts,
                command: "sleep 300",
                network: null
            );
            
            Assert.True(secondStart, "Second start should succeed");
            await Task.Delay(500);
            
            var statusSecond = await _dockerManager.GetContainerStatusAsync(_testContainerName);
            _output.WriteLine($"Status after second start: {statusSecond}");
            Assert.Equal("running", statusSecond);
            
            _output.WriteLine("TEST PASSED: Node restart properly updated status");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDataDir))
            {
                try
                {
                    Directory.Delete(testDataDir, true);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Warning: Could not delete test data directory: {ex.Message}");
                }
            }
        }
    }
}
