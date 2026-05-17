using PocxWallet.Core.Wallet;
using Xunit;

namespace PocxWallet.Tests;

public class MnemonicTests
{
    [Fact]
    public void TestMnemonicDerivationMatchesPhoenixPocx()
    {
        // 24-word mnemonic and password
        string phrase = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art";
        string password = "my_secret_password";

        // Create the wallet using from Mnemonic
        var wallet = HDWallet.FromMnemonic(phrase, password);

        // Get the first address (mainnet)
        var addressMainnet = wallet.GetPoCXAddress(0, 0, false, false);
        // Get the first address (testnet)
        var addressTestnet = wallet.GetPoCXAddress(0, 0, true, false);

        // Check prefixes
        Assert.StartsWith("pocx1q", addressMainnet);
        Assert.StartsWith("tpocx1q", addressTestnet);
        
        // Ensure that the output matches phoenix-pocx
        Assert.Equal("tpocx1qrgvk9pp3pr8ww3nuhkrjym5uwlypz4dfd3xzlq", addressTestnet);

        // NBitcoin implements standard BIP39 mnemonic to seed, and standard BIP32/BIP84 path
        // In phoenix-pocx, derivation for mainnet is m/84'/0'/0' and testnet is m/84'/1'/0'
        // And witness version 0, payload is hash160 of public key.
        // We ensure that we don't throw and the wallet properties work
        Assert.NotNull(addressMainnet);
        Assert.NotNull(addressTestnet);
    }
}
