using PocxWallet.Core.Wallet;
using NBitcoin;

namespace PocxWallet.Cli;

/// <summary>
/// Non-interactive demo to showcase wallet functionality
/// </summary>
public static class Demo
{
    public static void RunWalletDemo()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("PoCX Wallet - Feature Demonstration");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Demo 1: Create a new wallet
        Console.WriteLine("[Demo 1] Creating a new HD wallet");
        Console.WriteLine("-".PadRight(80, '-'));
        
        var wallet = HDWallet.CreateNew(WordCount.Twelve);
        Console.WriteLine($"√ Wallet created successfully!");
        Console.WriteLine($"  Mnemonic: {wallet.MnemonicPhrase}");
        Console.WriteLine($"  Address: {wallet.GetPoCXAddress(0, 0)}");
        Console.WriteLine();
        Console.WriteLine($"  WIF Mainnet: {wallet.GetWIFMainnet(0, 0)}");
        Console.WriteLine($"  WIF Testnet: {wallet.GetWIFTestnet(0, 0)}");
        Console.WriteLine();
        Console.WriteLine($"  Descriptor (Mainnet): {wallet.GetDescriptorMainnet(0, 0)}");
        Console.WriteLine($"  Descriptor (Testnet): {wallet.GetDescriptorTestnet(0, 0)}");
        Console.WriteLine();

        // Demo 2: Generate addresses
        Console.WriteLine("[Demo 2] Generating PoCX addresses");
        Console.WriteLine("-".PadRight(80, '-'));
        
        for (uint i = 0; i < 5; i++)
        {
            var address = wallet.GetPoCXAddress(0, i);
            var pubKey = wallet.GetPublicKey(0, i);
            Console.WriteLine($"  Address {i}: {address}");
            Console.WriteLine($"  Public Key: {pubKey[..32]}...");
        }
        Console.WriteLine();

        // Demo 3: Restore wallet from mnemonic
        Console.WriteLine("[Demo 3] Restoring wallet from mnemonic");
        Console.WriteLine("-".PadRight(80, '-'));
        
        var restoredWallet = HDWallet.FromMnemonic(wallet.MnemonicPhrase);
        var originalAddress = wallet.GetPoCXAddress(0, 0);
        var restoredAddress = restoredWallet.GetPoCXAddress(0, 0);
        
        Console.WriteLine($"  Original Address: {originalAddress}");
        Console.WriteLine($"  Restored Address: {restoredAddress}");
        Console.WriteLine($"  Match: {originalAddress == restoredAddress} √");
        Console.WriteLine();

        // Demo 4: Different accounts
        Console.WriteLine("[Demo 4] Multiple accounts from same seed");
        Console.WriteLine("-".PadRight(80, '-'));
        
        for (uint account = 0; account < 3; account++)
        {
            var address = wallet.GetPoCXAddress(account, 0);
            Console.WriteLine($"  Account {account}: {address}");
        }
        Console.WriteLine();

        // Demo 5: Export wallet
        Console.WriteLine("[Demo 5] Exporting wallet to JSON");
        Console.WriteLine("-".PadRight(80, '-'));
        
        var json = wallet.ExportToJson();
        Console.WriteLine(json);
        Console.WriteLine();

        // Demo 6: Using passphrase
        Console.WriteLine("[Demo 6] Creating wallet with passphrase");
        Console.WriteLine("-".PadRight(80, '-'));
        
        var walletWithPassphrase = HDWallet.CreateNew(WordCount.Twelve, "super-secret-passphrase");
        Console.WriteLine($"√ Wallet with passphrase created");
        Console.WriteLine($"  Address: {walletWithPassphrase.GetPoCXAddress(0, 0)}");
        Console.WriteLine($"  Note: Same mnemonic + different passphrase = different addresses");
        Console.WriteLine();

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Demo completed successfully! √");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
        Console.WriteLine("▲ SECURITY WARNING:");
        Console.WriteLine("   The mnemonic phrases shown above are for DEMONSTRATION ONLY.");
        Console.WriteLine("   Never share your real mnemonic phrase with anyone!");
        Console.WriteLine("   Always store it securely offline.");
        Console.WriteLine();
    }
}
