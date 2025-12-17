using ReactiveUI;
using System.Reactive;

namespace PocxWallet.UI.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private decimal _balance;
    private string _currentAddress = string.Empty;
    private int _blockHeight;
    private bool _isNodeConnected;
    
    public DashboardViewModel()
    {
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshData);
        
        // Initialize with demo data
        Balance = 0.0m;
        CurrentAddress = "pocx1q...";
        BlockHeight = 0;
        IsNodeConnected = false;
    }
    
    public decimal Balance
    {
        get => _balance;
        set => this.RaiseAndSetIfChanged(ref _balance, value);
    }
    
    public string CurrentAddress
    {
        get => _currentAddress;
        set => this.RaiseAndSetIfChanged(ref _currentAddress, value);
    }
    
    public int BlockHeight
    {
        get => _blockHeight;
        set => this.RaiseAndSetIfChanged(ref _blockHeight, value);
    }
    
    public bool IsNodeConnected
    {
        get => _isNodeConnected;
        set => this.RaiseAndSetIfChanged(ref _isNodeConnected, value);
    }
    
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    
    private async Task RefreshData()
    {
        // TODO: Implement actual data fetching
        await Task.Delay(100);
    }
}
