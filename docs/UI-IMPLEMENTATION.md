# PoCX Wallet - Avalonia UI Documentation

## Overview

The PoCX Wallet now includes a modern, cross-platform graphical user interface built with Avalonia UI Framework. This provides a consistent experience across Windows, Linux, and macOS platforms.

## Screenshots

### Main Window with Wallet Dashboard
The main window features a dark-themed interface with:
- **Left Sidebar**: Navigation menu with PoCX logo and version
  - Wallet (📁)
  - Node (🖥️)
  - Plot (📊)
  - Mine (⛏️)
  - Aggregator (🔗)
  - Electrs (⚡)
  - Settings (⚙️)
  - About (ℹ️)

- **Main Content Area**: Context-dependent views
  - Wallet Dashboard showing balance and quick actions
  - Service status and logs
  - Settings and configuration

### Theme Colors
- **Primary**: Deep blue/teal gradient (#0D1B2A to #1B4965)
- **Accent**: Bright cyan (#00D9FF) for buttons and highlights
- **Background**: Dark gradient (#0A0F1A to #1A1A2E)
- **Cards**: Semi-transparent panels (#1E2A3A with 80% opacity)

## Architecture

### Project Structure
```
PocxWallet.sln
├── PocxWallet.Core/              # Core wallet logic (existing)
├── PocxWallet.Cli/               # CLI application (existing)
├── PocxWallet.UI/                # NEW - Shared UI library
│   ├── ViewModels/               # MVVM view models
│   ├── Views/                    # AXAML views
│   ├── Models/                   # UI data models
│   ├── Services/                 # Service interfaces
│   ├── Styles/                   # Theme and colors
│   └── Assets/                   # Icons and images
├── PocxWallet.Desktop/           # NEW - Desktop launcher
└── PocxWallet.Android/           # PLANNED - Android launcher
```

### MVVM Pattern
- **ViewModels**: Business logic and state management
  - `MainWindowViewModel`: Navigation and app state
  - `WalletViewModel`: Wallet operations
  - `ServiceViewModel`: Service management (Node, Plotter, Miner, etc.)
  - `SettingsViewModel`: Application settings
  - `AboutViewModel`: About information

- **Views**: AXAML UI definitions
  - `MainWindow`: Main application window with navigation
  - `WalletView`: Wallet dashboard and operations
  - `ServiceView`: Service status, logs, and controls
  - `SettingsView`: Settings configuration
  - `AboutView`: About and license information

- **Services**: Abstraction layer
  - `IWalletService`: Wallet operations (load, save, balance)
  - `IDockerService`: Container management
  - `IConfigurationService`: Services.yaml configuration

## Features

### Wallet Management
- **Dashboard**: View active wallet balance and recent transactions
- **Quick Actions**: Send, Receive, Create, Import wallets
- **Wallet Info**: Addresses, UTXOs, blockchain information
- **Transaction Builder**: Create, sign, and broadcast transactions
- **PSBT Support**: Create and decode Partially Signed Bitcoin Transactions

### Service Management
All services (Node, Plotter, Miner, Aggregator, Electrs) share a common interface:
- **Status Display**: Running/Stopped with color indicator
- **Start/Stop Controls**: Toggle service state
- **Logs Viewer**: Real-time log streaming with auto-scroll
- **Parameters**: Configure service parameters from services.yaml
- **Settings**: Service-specific configuration

### Node Features
- Network mode indicator (Mainnet/Testnet/Regtest)
- Sync progress tracking
- Peer count
- Block height
- RPC configuration

### Miner Features  
- Active plots display
- Current deadline tracking
- Mining status (scanning, submitting, idle)
- Capacity statistics

### Plotter Features
- Plot creation progress
- Plot file management
- Disk space indicators

## Building and Running

### Prerequisites
- .NET 9.0 SDK
- For Android: Android SDK with .NET workload

### Build All Projects
```bash
cd /path/to/pocx-wallet
dotnet restore
dotnet build
```

### Run Desktop Application

**Windows:**
```bash
dotnet run --project PocxWallet.Desktop
```

**Linux:**
```bash
dotnet run --project PocxWallet.Desktop
```

**macOS:**
```bash
dotnet run --project PocxWallet.Desktop
```

### Publish Self-Contained Executable

**Windows:**
```bash
dotnet publish PocxWallet.Desktop -c Release -r win-x64 --self-contained
```

**Linux:**
```bash
dotnet publish PocxWallet.Desktop -c Release -r linux-x64 --self-contained
```

**macOS:**
```bash
dotnet publish PocxWallet.Desktop -c Release -r osx-x64 --self-contained
```

## Development

### Adding New Views
1. Create ViewModel in `PocxWallet.UI/ViewModels/`:
   ```csharp
   public partial class MyViewModel : ViewModelBase
   {
       [ObservableProperty]
       private string _myProperty = "";
       
       [RelayCommand]
       private void MyAction()
       {
           // Action logic
       }
   }
   ```

2. Create View in `PocxWallet.UI/Views/`:
   ```xml
   <UserControl xmlns="https://github.com/avaloniaui"
                x:Class="PocxWallet.UI.Views.MyView"
                x:DataType="vm:MyViewModel">
       <TextBlock Text="{Binding MyProperty}" />
   </UserControl>
   ```

3. Register in MainWindowViewModel navigation

### Styling
Use predefined theme classes:
- `Classes="heading1"` - Large headings
- `Classes="heading2"` - Section headings
- `Classes="card"` - Panel/card styling
- `Classes="primary"` - Primary action buttons
- `Classes="secondary"` - Secondary buttons

### Colors
Access theme colors via StaticResource:
- `{StaticResource PocxAccentBrush}`
- `{StaticResource PocxPrimaryBrush}`
- `{StaticResource PocxTextPrimaryBrush}`
- `{StaticResource PocxBackgroundBrush}`

## Acceptance Criteria Status

✅ Solution builds successfully with `dotnet build`
✅ Desktop app project created for Windows and Linux
✅ All CLI wallet functions have UI placeholders
✅ Service management views implemented
✅ Dark theme with PoCX brand colors applied
✅ Responsive layout with navigation sidebar
⏳ Android APK builds (planned for future release)
⏳ Copy-to-clipboard functionality (to be implemented)
⏳ Full wallet integration (stub implementations ready)
⏳ Docker service integration (stub implementations ready)

## Future Enhancements
- Complete wallet integration with PocxWallet.Cli.Configuration.WalletManager
- Implement Docker service management integration
- Add QR code generation for receiving addresses
- Transaction history with filtering and search
- Plot file browser with verification status
- Mining statistics dashboard with charts
- Network peer map visualization
- Android mobile application
- Hardware wallet support
- Multi-language localization
