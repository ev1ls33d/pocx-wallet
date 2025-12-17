# PoCX Wallet UI

Cross-platform UI for PoCX Wallet built with Avalonia and ReactiveUI.

## Architecture

The UI follows the MVVM (Model-View-ViewModel) pattern using ReactiveUI for reactive programming:

```
PocxWallet.UI/
├── PocxWallet.UI/              # Shared UI library
│   ├── ViewModels/             # View models with ReactiveUI
│   ├── Views/                  # AXAML views
│   ├── Assets/                 # Images and resources
│   └── App.axaml               # Application definition
├── PocxWallet.UI.Desktop/      # Desktop application (Windows, Linux, macOS)
├── PocxWallet.UI.Android/      # Android application
├── PocxWallet.UI.iOS/          # iOS application
└── PocxWallet.UI.Browser/      # WebAssembly browser application
```

## Features Implemented

### Core Views
- **Dashboard**: Overview of balance, addresses, and network status
- **Wallet**: Create and restore wallets, view addresses
- **Send**: Send transactions with optional encrypted messages, multi-out support
- **Receive**: Display addresses with QR codes, create payment requests
- **Transactions**: View transaction history with filtering
- **Accounts**: Multi-account management with watch-only support
- **Contacts**: Address book for frequently used addresses
- **Settings**: Node configuration, language selection, theme, security

### UI Features
- Navigation sidebar with all major sections
- Reactive UI updates using ReactiveUI
- DataGrids for displaying lists (transactions, accounts, contacts)
- Form validation and command binding
- Responsive layouts

## Technologies

- **Avalonia 11.3**: Cross-platform UI framework
- **ReactiveUI 11.3**: MVVM framework with reactive extensions
- **QRCoder**: QR code generation
- **.NET 9.0**: Latest .NET runtime

## Running the Application

### Desktop
```bash
cd PocxWallet.UI/PocxWallet.UI.Desktop
dotnet run
```

### Browser (WebAssembly)
```bash
cd PocxWallet.UI/PocxWallet.UI.Browser
dotnet run
```

### Android
Requires Android SDK and emulator:
```bash
cd PocxWallet.UI/PocxWallet.UI.Android
dotnet build
# Deploy to emulator or device
```

### iOS
Requires macOS with Xcode:
```bash
cd PocxWallet.UI/PocxWallet.UI.iOS
dotnet build
# Deploy to simulator or device
```

## Building

### All Projects
```bash
dotnet build PocxWallet.UI.sln
```

### Desktop Only
```bash
dotnet build PocxWallet.UI/PocxWallet.UI.Desktop/PocxWallet.UI.Desktop.csproj
```

### Publishing
```bash
# Desktop - Self-contained executable
cd PocxWallet.UI/PocxWallet.UI.Desktop
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
```

## Development

### Adding New Views

1. Create ViewModel in `ViewModels/`:
```csharp
public class MyViewModel : ViewModelBase
{
    private string _myProperty;
    
    public string MyProperty
    {
        get => _myProperty;
        set => this.RaiseAndSetIfChanged(ref _myProperty, value);
    }
    
    public ReactiveCommand<Unit, Unit> MyCommand { get; }
    
    public MyViewModel()
    {
        MyCommand = ReactiveCommand.Create(DoSomething);
    }
    
    private void DoSomething()
    {
        // Implementation
    }
}
```

2. Create View in `Views/`:
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:PocxWallet.UI.ViewModels"
             x:Class="PocxWallet.UI.Views.MyView"
             x:DataType="vm:MyViewModel">
    <TextBlock Text="{Binding MyProperty}" />
    <Button Content="Click Me" Command="{Binding MyCommand}" />
</UserControl>
```

3. Add code-behind:
```csharp
public partial class MyView : UserControl
{
    public MyView()
    {
        InitializeComponent();
    }
}
```

4. Add navigation in `MainViewModel`:
```csharp
ShowMyViewCommand = ReactiveCommand.Create(() => CurrentPage = new MyViewModel());
```

### Styling

Avalonia uses Fluent theme by default. Styles can be customized in `App.axaml`:

```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="/Styles/Custom.axaml"/>
</Application.Styles>
```

## Phoenix Wallet Features Integration

This UI aims to provide feature parity with the Phoenix wallet for Signum Network:

### Implemented (UI Only)
- ✅ Multi-account support UI
- ✅ Contact management UI
- ✅ Transaction history view
- ✅ Send/receive views
- ✅ Settings with node configuration
- ✅ Dashboard overview

### To Implement
- [ ] QR code generation and scanning
- [ ] Multi-out payment transaction building
- [ ] Encrypted messaging
- [ ] Deep linking support
- [ ] Alias system
- [ ] Market information integration
- [ ] Blockchain explorer
- [ ] Biometric authentication (mobile)
- [ ] Push notifications (mobile)
- [ ] Internationalization (25+ languages)
- [ ] Theme switching (light/dark)
- [ ] Balance history charts

## Next Steps

1. **Service Layer Implementation**: Connect ViewModels to actual wallet services
2. **State Management**: Implement application state persistence
3. **Error Handling**: Add comprehensive error handling and user feedback
4. **Testing**: Create unit tests for ViewModels
5. **Mobile Optimization**: Optimize layouts for mobile devices
6. **Localization**: Add multi-language support
7. **Theme**: Implement light/dark theme switching

## Notes

- The UI is currently in foundation stage with basic views
- ViewModels contain placeholder implementations (TODO comments)
- Service layer needs to be connected for actual functionality
- Mobile projects are scaffolded but need platform-specific implementations
- Phoenix logo used as placeholder (can be replaced with PoCX branding)
