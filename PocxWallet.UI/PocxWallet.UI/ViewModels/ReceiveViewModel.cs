using ReactiveUI;
using System.Reactive;

namespace PocxWallet.UI.ViewModels;

public class ReceiveViewModel : ViewModelBase
{
    private string _address = string.Empty;
    private decimal _requestAmount;
    private string _message = string.Empty;
    private byte[]? _qrCodeImage;
    
    public ReceiveViewModel()
    {
        GenerateQRCommand = ReactiveCommand.Create(GenerateQRCode);
        CopyAddressCommand = ReactiveCommand.CreateFromTask(CopyAddress);
        
        // Load current address
        Address = "pocx1q20kypkuljzp8ulujfw67ryuwnh5h2d2ygadj23";
        GenerateQRCode();
    }
    
    public string Address
    {
        get => _address;
        set => this.RaiseAndSetIfChanged(ref _address, value);
    }
    
    public decimal RequestAmount
    {
        get => _requestAmount;
        set
        {
            this.RaiseAndSetIfChanged(ref _requestAmount, value);
            GenerateQRCode();
        }
    }
    
    public string Message
    {
        get => _message;
        set
        {
            this.RaiseAndSetIfChanged(ref _message, value);
            GenerateQRCode();
        }
    }
    
    public byte[]? QRCodeImage
    {
        get => _qrCodeImage;
        set => this.RaiseAndSetIfChanged(ref _qrCodeImage, value);
    }
    
    public ReactiveCommand<Unit, Unit> GenerateQRCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyAddressCommand { get; }
    
    private void GenerateQRCode()
    {
        // TODO: Implement QR code generation with QRCoder
        // For now, set to null
        QRCodeImage = null;
    }
    
    private async Task CopyAddress()
    {
        // TODO: Implement clipboard copy
        await Task.CompletedTask;
    }
}
