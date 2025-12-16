# Usage Guide

## Getting Started

### Step 1: Build PoCX Binaries

First, build the PoCX binaries from the submodule:

```bash
cd pocx
rustup toolchain install nightly
rustup override set nightly
cargo build --release
cd ..
```

### Step 2: Build the Wallet

```bash
dotnet build
```

### Step 3: Run the Application

```bash
cd PocxWallet.Cli
dotnet run
```

## Creating Your First Wallet

1. From the main menu, select **💰 Wallet Management**
2. Choose **Create New Wallet**
3. Select your preferred mnemonic word count (12 words recommended for beginners)
4. Optionally add a passphrase for extra security
5. **CRITICAL**: Write down your mnemonic phrase and store it securely offline
6. Save the wallet to a file when prompted

### Example Wallet Creation

```
Select mnemonic word count: 12 words
Use additional passphrase? No

Your Mnemonic Phrase:
abandon ability able about above absent absorb abstract absurd abuse access accident

Master Public Key: xpub...
Default Address: 123456789012345

⚠ IMPORTANT: Save your mnemonic phrase in a secure location!
```

## Generating Plot Files

Before you can mine, you need to create plot files:

1. From the main menu, select **📊 Plotting**
2. Choose **Create Plot**
3. Enter your account ID (from your wallet)
4. Specify where to store plots (e.g., `./plots`)
5. Enter number of warps (1 warp ≈ 1GB, start with 10 for testing)
6. Wait for plotting to complete (this can take a while)

### Plotting Time Estimates

- 10 warps (~10GB): ~1-2 hours on a modern CPU
- 100 warps (~100GB): ~10-20 hours
- 1000 warps (~1TB): ~100-200 hours

**Tip**: Use multiple CPU threads to speed up plotting (configured in PoCX)

## Setting Up Mining

### 1. Create a Miner Configuration

From the main menu:
1. Select **⛏️ Mining**
2. Choose **Create Miner Config**
3. Enter your mining pool details:
   - Pool URL: `http://pool.example.com:8080`
   - API path: `/pocx`
   - Your account ID: (from your wallet)
   - Plot directory: `./plots`
   - CPU threads: (number of cores to use)

This creates a `config.yaml` file.

### 2. Start Mining

1. Select **⛏️ Mining**
2. Choose **Start Mining**
3. Monitor the output for your deadlines and submissions
4. Press Ctrl+C or use "Stop Mining" to stop

### Mining Tips

- Start with a smaller plot (10-100GB) to test the setup
- Join a mining pool for more consistent rewards
- Monitor your deadlines - lower is better
- Keep your plots on fast storage (SSD recommended)
- Use multiple plot files for better performance

## Generating Vanity Addresses

Want an address with a specific pattern? Use the vanity generator:

1. From the main menu, select **✨ Vanity Address Generator**
2. Enter your desired pattern (e.g., "1337" or "COOL")
3. Wait for a matching address to be found
4. Save the wallet when done

### Vanity Generation Tips

- Shorter patterns are much faster to find
- Each additional character increases difficulty exponentially
- GPU acceleration (planned) will significantly speed this up
- Pattern matching is case-insensitive for numeric addresses

### Example Times (CPU-based estimates)

- 2 digits: seconds
- 3 digits: minutes
- 4 digits: tens of minutes
- 5 digits: hours
- 6+ digits: days to weeks

## Verifying Plot Files

To check if your plot files are valid:

1. Select **📊 Plotting**
2. Choose **Verify Plot**
3. Enter the path to your plot file
4. Wait for verification to complete

## Configuration

### Application Settings

Edit `appsettings.json`:

```json
{
  "PoCXBinariesPath": "./pocx/target/release",
  "PlotDirectory": "./plots",
  "WalletFilePath": "./wallet.json",
  "MinerConfigPath": "./config.yaml"
}
```

Or use the Settings menu in the CLI.

### Miner Configuration

Edit `config.yaml`:

```yaml
chains:
  - name: "my_pool"
    base_url: "http://pool.example.com:8080"
    api_path: "/pocx"
    accounts:
      - account: "YOUR_ACCOUNT_ID"

plot_dirs:
  - "./plots"

cpu_threads: 8
hdd_use_direct_io: true
show_progress: true
```

## Common Commands

### Show Wallet Addresses

To view multiple addresses from your wallet:

1. Select **💰 Wallet Management**
2. Choose **Show Addresses**
3. Enter the path to your wallet file
4. Specify how many addresses to display

This is useful for:
- Generating multiple addresses for different uses
- Finding a specific address index
- Verifying your wallet is working correctly

## Security Best Practices

### Protecting Your Mnemonic

- ✅ Write it down on paper
- ✅ Store it in a fireproof safe
- ✅ Make multiple copies in different locations
- ✅ Never store it digitally without encryption
- ✅ Never share it with anyone
- ❌ Don't store it in plain text files
- ❌ Don't email it to yourself
- ❌ Don't take a photo of it
- ❌ Don't store it in the cloud

### Wallet File Security

The wallet JSON file contains your mnemonic. Treat it as sensitive:

```bash
# Set restrictive permissions (Linux/macOS)
chmod 600 wallet.json

# Consider encrypting it
gpg --symmetric wallet.json
```

### Using Passphrases

A passphrase adds an additional layer of security:

- Must be remembered - if forgotten, your wallet is lost
- Makes your mnemonic useless without the passphrase
- Recommended for large amounts
- Use a strong, unique passphrase

## Troubleshooting

### "Plotter binary not found"

The PoCX binaries haven't been built. Run:

```bash
cd pocx
cargo build --release
cd ..
```

### "Config file not found"

Create a config.yaml file using the "Create Miner Config" option or copy from config.yaml.example.

### "Plot file not found"

Ensure you've created plots using the plotting menu before mining.

### Mining shows no activity

Check:
1. Your config.yaml is correct
2. Your plots exist in the specified directories
3. The pool URL is accessible
4. Your account ID is correct

## Advanced Usage

### Multiple Accounts

You can generate multiple accounts from the same mnemonic:

```csharp
var wallet = HDWallet.FromMnemonic("your mnemonic phrase");
var address0 = wallet.GetPoCXAddress(0, 0); // Account 0, Address 0
var address1 = wallet.GetPoCXAddress(1, 0); // Account 1, Address 0
var address2 = wallet.GetPoCXAddress(0, 1); // Account 0, Address 1
```

### Scripting

You can create scripts using the wallet library:

```csharp
using PocxWallet.Core.Wallet;

// Create wallet programmatically
var wallet = HDWallet.CreateNew();
Console.WriteLine($"Mnemonic: {wallet.MnemonicPhrase}");
Console.WriteLine($"Address: {wallet.GetPoCXAddress()}");
```

## Next Steps

1. **Test with Small Amounts**: Start with small plots and test mining
2. **Join a Pool**: Solo mining requires significant storage
3. **Scale Up**: Once comfortable, increase plot sizes
4. **Monitor Performance**: Track deadlines and earnings
5. **Backup Everything**: Keep multiple backups of your mnemonic

## Getting Help

- Check the [README.md](README.md) for installation details
- Review the [PoCX Documentation](https://github.com/PoC-Consortium/pocx)
- Open an issue on GitHub for bugs or questions

## Disclaimer

This software is experimental. Always:
- Test with small amounts first
- Keep backups of your mnemonic
- Use at your own risk
- Verify all transactions

Happy mining! 🎉
