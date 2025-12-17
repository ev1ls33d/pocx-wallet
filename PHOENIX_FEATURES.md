# Phoenix Wallet Features Comparison

This document compares features from the [Phoenix wallet](https://github.com/signum-network/phoenix) for Signum Network with the PoCX Wallet implementation.

## Feature Matrix

| Feature | Phoenix (Signum) | PoCX Wallet Status | Notes |
|---------|------------------|-------------------|-------|
| **Security** |
| Private key encryption | ✅ | 🟡 Interface defined | PIN-based encryption with hashed PIN |
| Biometric authentication | ✅ | 🟡 UI ready | Mobile only - needs platform implementation |
| Local transaction signing | ✅ | ✅ | Implemented in core |
| **Account Management** |
| Multi-account support | ✅ | 🟡 UI + Interface | Service implementation needed |
| Offline/watch-only accounts | ✅ | 🟡 UI + Interface | Service implementation needed |
| Contact list | ✅ | 🟡 UI + Interface | Service implementation needed |
| Account profiles | ✅ | 🔴 Planned | Not yet started |
| **Payments** |
| Single payments | ✅ | ✅ | Core functionality exists |
| Multi-out payments | ✅ | 🟡 UI created | Transaction building needed |
| QR code display | ✅ | 🟡 Package added | Generation logic needed |
| QR code scanning | ✅ | 🔴 Planned | Mobile camera access required |
| **Messaging** |
| Encrypted messages | ✅ | 🔴 Planned | Encryption logic needed |
| Unencrypted messages | ✅ | 🔴 Planned | Basic implementation |
| Message viewing | ✅ | 🔴 Planned | UI exists, logic needed |
| **Mining** |
| Mining setup | ✅ | ✅ | CLI implementation exists |
| Reward recipient | ✅ | ✅ | Via miner configuration |
| PoC+ commitment | N/A | N/A | Signum-specific feature |
| **Network** |
| Node selection | ✅ | 🟡 UI created | Node list needed |
| Custom nodes | ✅ | 🟡 UI created | Implementation needed |
| Auto-select fastest | ✅ | 🟡 UI created | Speed test logic needed |
| View peers | ✅ | 🟡 Interface defined | Service implementation needed |
| View blocks | ✅ | 🟡 Interface defined | Service implementation needed |
| View transactions | ✅ | 🟡 UI + Interface | Service implementation needed |
| **Assets & Tokens** |
| SRC22 tokens | ✅ | N/A | Signum-specific |
| SRC44 descriptors | ✅ | N/A | Signum-specific |
| Multi-asset transfer | ✅ | N/A | Signum-specific |
| **User Experience** |
| Dashboard layouts | ✅ | 🟡 Basic UI | Multiple layouts planned |
| Market data | ✅ | 🔴 Planned | API integration needed |
| Balance history | ✅ | 🔴 Planned | Chart library needed |
| Push notifications | ✅ | 🔴 Planned | Mobile only |
| Localization (25+ languages) | ✅ | 🔴 Planned | i18n framework needed |
| Light/dark theme | ✅ | 🔴 Planned | Avalonia theme switching |
| **Platform Support** |
| Desktop (Win/Mac/Linux) | ✅ | 🟡 Scaffolded | Builds successfully |
| Mobile (Android) | ✅ | 🟡 Scaffolded | Needs testing |
| Mobile (iOS) | ✅ | 🟡 Scaffolded | Needs testing |
| Web (Browser) | ✅ | 🟡 Scaffolded | WebAssembly support |
| **Additional** |
| Deep linking | ✅ | 🔴 Planned | Protocol handler needed |
| Alias system | ✅ | 🔴 Planned | Signum feature adaptation |
| Auto updates | ✅ | 🔴 Planned | Update mechanism needed |

**Legend:**
- ✅ Fully implemented
- 🟡 Partially implemented (UI/interface exists, needs logic)
- 🔴 Planned but not started
- N/A Not applicable to PoCX

## Architecture Comparison

### Phoenix (Signum)
```
Angular 8 Web App
├── @signumjs/* libraries
├── NgRx state management
├── Angular Material UI
└── Electron wrapper (desktop)

React Native Mobile App
├── @signumjs/* libraries
└── Native mobile components
```

### PoCX Wallet
```
.NET 9 Application
├── PocxWallet.Core (NBitcoin)
├── PocxWallet.UI (Avalonia + ReactiveUI)
│   ├── Desktop (all platforms)
│   ├── Android
│   ├── iOS
│   └── Browser (WebAssembly)
└── PocxWallet.Cli (Spectre.Console)
```

## Key Differences

### Technology Stack
- **Phoenix**: JavaScript/TypeScript ecosystem (Angular + React Native)
- **PoCX**: C#/.NET ecosystem (Avalonia for true cross-platform)

### Blockchain
- **Phoenix**: Signum Network (formerly Burstcoin)
- **PoCX**: PoCX (Proof of Capacity X) - Bitcoin-based

### Unique PoCX Features
1. **HD Wallet**: BIP39/BIP32/BIP44 compliant (Bitcoin-style)
2. **Bech32 Addresses**: pocx1q... format (SegWit-compatible)
3. **Bitcoin-PoCX Node**: Full node integration
4. **Plotting**: Native PoCX plot file generation
5. **Mining**: Direct miner wrapper integration
6. **Vanity Addresses**: GPU-accelerated generation

### Unique Phoenix Features
1. **SRC22/SRC44**: Token and descriptor standards
2. **Aliases**: On-chain name system
3. **PoC+ Commitment**: Signum consensus enhancement
4. **Smart Contracts**: Signum smart contract support

## Implementation Roadmap

### Phase 1: Foundation ✅
- [x] Service layer interfaces
- [x] Avalonia UI with ReactiveUI
- [x] Basic views (Dashboard, Send, Receive, etc.)
- [x] Cross-platform project structure

### Phase 2: Core Services (Current)
- [ ] Implement IWalletService
- [ ] Implement ITransactionService
- [ ] Implement INodeService
- [ ] Implement IAccountService
- [ ] Implement IContactService
- [ ] Connect ViewModels to services

### Phase 3: Transaction Features
- [ ] Multi-out transaction builder
- [ ] Message encryption/decryption
- [ ] QR code generation
- [ ] Transaction history with filters
- [ ] Fee estimation

### Phase 4: Advanced Features
- [ ] Multi-account management
- [ ] Watch-only accounts
- [ ] Contact management with search
- [ ] Alias system (adapted for PoCX)
- [ ] Deep linking

### Phase 5: UX Enhancements
- [ ] Dashboard layouts (multiple options)
- [ ] Market data integration
- [ ] Balance history charts
- [ ] Theme switching (light/dark)
- [ ] Internationalization

### Phase 6: Mobile Optimization
- [ ] Touch-optimized UI
- [ ] Biometric authentication
- [ ] QR scanner (camera integration)
- [ ] Push notifications
- [ ] Battery optimization

### Phase 7: Network Integration
- [ ] Node list with health checks
- [ ] Auto-select fastest node
- [ ] Peer explorer
- [ ] Block explorer
- [ ] Network statistics

### Phase 8: Security & Updates
- [ ] PIN-based wallet encryption
- [ ] Secure storage (platform-specific)
- [ ] Auto-update mechanism
- [ ] Security audit

## Electrs Integration (Mobile)

For mobile devices where running a full node is not feasible:

### Background
[Electrs](https://github.com/romanz/electrs) is a lightweight Electrum server that allows mobile wallets to query blockchain data without running a full node.

### Implementation Plan
1. **Research**: Determine if PoCX is compatible with Electrum protocol
2. **Server**: Run electrs connected to Bitcoin-PoCX node
3. **Client**: Implement Electrum client in PocxWallet.Core
4. **Mobile Mode**: Add "Light Client" mode in mobile apps
5. **Fallback**: Auto-detect and switch between full node and electrs

### Benefits
- Reduced storage requirements on mobile
- Lower bandwidth usage
- Faster sync times
- Better battery life
- Maintain privacy (with own electrs server)

## Testing Strategy

### Unit Tests
- [ ] Core wallet functions
- [ ] Service implementations
- [ ] ViewModels logic
- [ ] Utility functions

### Integration Tests
- [ ] Node communication
- [ ] Transaction creation
- [ ] Wallet import/export
- [ ] Multi-account operations

### Platform Tests
- [ ] Desktop (Windows, Linux, macOS)
- [ ] Android (various versions)
- [ ] iOS (various versions)
- [ ] Browser (WASM)

### User Acceptance Tests
- [ ] Wallet creation workflow
- [ ] Send transaction workflow
- [ ] Receive workflow
- [ ] Account management
- [ ] Settings configuration

## Documentation Needs

- [ ] API documentation (service interfaces)
- [ ] User guide (wallet operations)
- [ ] Developer guide (contributing)
- [ ] Mobile deployment guide
- [ ] Security best practices
- [ ] Troubleshooting guide

## Timeline Estimate

Based on Phoenix's feature set and current progress:

- **Phase 1 (Foundation)**: ✅ Complete (Week 1)
- **Phase 2 (Core Services)**: 2-3 weeks
- **Phase 3 (Transactions)**: 2 weeks
- **Phase 4 (Advanced)**: 2-3 weeks
- **Phase 5 (UX)**: 2 weeks
- **Phase 6 (Mobile)**: 3-4 weeks
- **Phase 7 (Network)**: 1-2 weeks
- **Phase 8 (Security)**: 1-2 weeks

**Total Estimated Time**: 13-19 weeks for feature parity

## Conclusion

The PoCX Wallet UI foundation is now in place with Avalonia and ReactiveUI. The architecture follows industry best practices with clear separation of concerns through the service layer. While many features are planned, the foundation is solid and ready for implementation.

The main advantages over Phoenix's architecture are:
1. **True cross-platform**: Single codebase for all platforms (including mobile)
2. **Modern .NET**: Leveraging .NET 9 performance and features
3. **Strong typing**: C# provides better tooling and type safety
4. **Bitcoin compatibility**: Closer to Bitcoin ecosystem standards

Next steps focus on implementing the service layer to connect the UI to the existing wallet functionality, followed by adding Phoenix-inspired features adapted for the PoCX blockchain.
