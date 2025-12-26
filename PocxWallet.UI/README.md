# PoCX Wallet - Avalonia UI

This directory contains the Avalonia-based cross-platform UI for PoCX Wallet.

## Projects

### PocxWallet.UI
Shared UI library containing:
- **ViewModels**: MVVM view models for all sections
- **Views**: AXAML views for wallet, services, and settings
- **Models**: Data models for UI state
- **Services**: Service interfaces and implementations
- **Styles**: Theme and color definitions with PoCX branding
- **Assets**: Icons and images

### PocxWallet.Desktop
Desktop launcher for Windows, Linux, and macOS platforms.

### PocxWallet.Android
Android launcher project (to be implemented).

## Building

### Prerequisites
- .NET 9.0 SDK
- For Android builds: Android SDK

### Build All Projects
```bash
dotnet build
```

### Run Desktop App
```bash
dotnet run --project PocxWallet.Desktop
```

### Build Android APK
```bash
dotnet build PocxWallet.Android
```

## UI Design

### Theme
The UI follows a "Proof of Capacity" blockchain aesthetic with:
- **Primary Colors**: Deep blue/teal (#0D1B2A to #1B4965)
- **Accent Color**: Bright cyan (#00D9FF)
- **Dark Background**: Gradient from #0A0F1A to #1A1A2E
- **Cards**: Semi-transparent dark panels with glassmorphism

### Navigation Structure
- **Wallet**: Dashboard, balance, transactions, wallet management
- **Node**: Bitcoin-PoCX node control and monitoring
- **Plot**: Plot file creation and management
- **Mine**: Mining status and statistics
- **Aggregator**: Mining aggregator configuration
- **Electrs**: Electrum server management
- **Settings**: Application settings
- **About**: Version and license information

## Architecture

### MVVM Pattern
The UI uses the Model-View-ViewModel (MVVM) pattern with:
- **CommunityToolkit.Mvvm**: For observable properties and commands
- **Avalonia data binding**: For reactive UI updates
- **Service interfaces**: For decoupling business logic from UI

### Service Layer
- **IWalletService**: Wallet operations (load, save, balance)
- **IDockerService**: Docker container management
- **IConfigurationService**: Services.yaml configuration

### Platform Support
- **Windows**: Full desktop support with native rendering
- **Linux**: X11/Wayland desktop support
- **macOS**: Native macOS rendering
- **Android**: Mobile-optimized layouts (planned)

## Contributing

When adding new views:
1. Create ViewModel in `ViewModels/` inheriting from `ViewModelBase`
2. Create View in `Views/` with corresponding `.axaml` and `.axaml.cs`
3. Use the defined theme styles from `Styles/Theme.axaml`
4. Follow the PoCX color palette for consistency
