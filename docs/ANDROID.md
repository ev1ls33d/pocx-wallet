# Android Support (Planned)

The PoCX Wallet Android application is planned for future implementation.

## Requirements
- .NET 9.0 with Android workload
- Android SDK
- Android 5.0 (API 21) or higher

## Implementation Notes

When implementing the Android project:

1. Create project with: `dotnet new avalonia.xplat -n PocxWallet.Android`
2. Reference PocxWallet.UI project
3. Configure AndroidManifest.xml with required permissions
4. Implement mobile-specific layouts:
   - Bottom navigation instead of sidebar
   - Single-panel views
   - Touch-optimized controls
5. Add adaptive icons and splash screen

## Key Considerations
- Mobile-optimized layouts for smaller screens
- Touch gestures for navigation
- Background service restrictions
- Reduced resource usage for battery life
- Secure storage for private keys

## Build Command (Future)
```bash
dotnet build PocxWallet.Android -f net9.0-android
```
