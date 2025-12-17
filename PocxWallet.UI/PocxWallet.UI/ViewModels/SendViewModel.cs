using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;

namespace PocxWallet.UI.ViewModels;

public class SendViewModel : ViewModelBase
{
    private string _recipientAddress = string.Empty;
    private decimal _amount;
    private string _message = string.Empty;
    private bool _encryptMessage;
    private bool _isMultiOut;
    
    public SendViewModel()
    {
        SendCommand = ReactiveCommand.CreateFromTask(Send, 
            this.WhenAnyValue(x => x.RecipientAddress, x => x.Amount,
                (addr, amt) => !string.IsNullOrEmpty(addr) && amt > 0));
        
        AddRecipientCommand = ReactiveCommand.Create(AddRecipient);
        RemoveRecipientCommand = ReactiveCommand.Create<MultiOutRecipient>(RemoveRecipient);
        
        Recipients = new ObservableCollection<MultiOutRecipient>();
    }
    
    public string RecipientAddress
    {
        get => _recipientAddress;
        set => this.RaiseAndSetIfChanged(ref _recipientAddress, value);
    }
    
    public decimal Amount
    {
        get => _amount;
        set => this.RaiseAndSetIfChanged(ref _amount, value);
    }
    
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }
    
    public bool EncryptMessage
    {
        get => _encryptMessage;
        set => this.RaiseAndSetIfChanged(ref _encryptMessage, value);
    }
    
    public bool IsMultiOut
    {
        get => _isMultiOut;
        set => this.RaiseAndSetIfChanged(ref _isMultiOut, value);
    }
    
    public ObservableCollection<MultiOutRecipient> Recipients { get; }
    
    public ReactiveCommand<Unit, Unit> SendCommand { get; }
    public ReactiveCommand<Unit, Unit> AddRecipientCommand { get; }
    public ReactiveCommand<MultiOutRecipient, Unit> RemoveRecipientCommand { get; }
    
    private async Task Send()
    {
        // TODO: Implement transaction sending
        await Task.Delay(100);
    }
    
    private void AddRecipient()
    {
        Recipients.Add(new MultiOutRecipient());
    }
    
    private void RemoveRecipient(MultiOutRecipient recipient)
    {
        Recipients.Remove(recipient);
    }
}

public class MultiOutRecipient : ReactiveObject
{
    private string _address = string.Empty;
    private decimal _amount;
    
    public string Address
    {
        get => _address;
        set => this.RaiseAndSetIfChanged(ref _address, value);
    }
    
    public decimal Amount
    {
        get => _amount;
        set => this.RaiseAndSetIfChanged(ref _amount, value);
    }
}
