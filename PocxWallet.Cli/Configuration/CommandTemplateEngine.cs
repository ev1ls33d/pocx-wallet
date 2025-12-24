using System.Text.RegularExpressions;
using PocxWallet.Cli.Resources;
using PocxWallet.Core.Wallet;
using Spectre.Console;

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Template engine for processing custom command templates.
/// Supports user input placeholders and macro function calls.
/// </summary>
public class CommandTemplateEngine
{
    private readonly Dictionary<string, Func<string[], string>> _macros = new();
    private readonly Func<HDWallet?> _walletProvider;

    /// <summary>
    /// Regex pattern for input placeholders: {{input:name}}
    /// </summary>
    private static readonly Regex InputPattern = new(@"\{\{input:(\w+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern for macro placeholders: {{macro:FunctionName(args)}}
    /// </summary>
    private static readonly Regex MacroPattern = new(@"\{\{macro:(\w+(?:\.\w+)?)\(([^)]*)\)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Timeout for user-provided regex patterns to prevent ReDoS attacks
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public CommandTemplateEngine(Func<HDWallet?> walletProvider)
    {
        _walletProvider = walletProvider;
        RegisterDefaultMacros();
    }

    /// <summary>
    /// Register default macros available in templates
    /// </summary>
    private void RegisterDefaultMacros()
    {
        // HDWallet.GetDescriptor(testnet) - returns descriptor with checksum
        _macros["HDWallet.GetDescriptor"] = args =>
        {
            var wallet = _walletProvider();
            if (wallet == null)
            {
                throw new InvalidOperationException(Strings.CustomActions.NoWalletLoaded);
            }

            var testnet = args.Length > 0 && bool.TryParse(args[0].Trim(), out var t) && t;
            return wallet.GetDescriptor(testnet);
        };

        // HDWallet.GetAddress(testnet, account, index) - returns PoCX address
        _macros["HDWallet.GetAddress"] = args =>
        {
            var wallet = _walletProvider();
            if (wallet == null)
            {
                throw new InvalidOperationException(Strings.CustomActions.NoWalletLoaded);
            }

            var testnet = args.Length > 0 && bool.TryParse(args[0].Trim(), out var t) && t;
            uint account = args.Length > 1 && uint.TryParse(args[1].Trim(), out var a) ? a : 0;
            uint index = args.Length > 2 && uint.TryParse(args[2].Trim(), out var i) ? i : 0;
            return wallet.GetPoCXAddress(account, index, testnet);
        };

        // HDWallet.GetWIF(testnet, account, index) - returns WIF private key
        _macros["HDWallet.GetWIF"] = args =>
        {
            var wallet = _walletProvider();
            if (wallet == null)
            {
                throw new InvalidOperationException(Strings.CustomActions.NoWalletLoaded);
            }

            var testnet = args.Length > 0 && bool.TryParse(args[0].Trim(), out var t) && t;
            uint account = args.Length > 1 && uint.TryParse(args[1].Trim(), out var a) ? a : 0;
            uint index = args.Length > 2 && uint.TryParse(args[2].Trim(), out var i) ? i : 0;
            return testnet ? wallet.GetWIFTestnet(account, index) : wallet.GetWIFMainnet(account, index);
        };

        // HDWallet.GetPublicKey(testnet, account, index) - returns public key hex
        _macros["HDWallet.GetPublicKey"] = args =>
        {
            var wallet = _walletProvider();
            if (wallet == null)
            {
                throw new InvalidOperationException(Strings.CustomActions.NoWalletLoaded);
            }

            var testnet = args.Length > 0 && bool.TryParse(args[0].Trim(), out var t) && t;
            uint account = args.Length > 1 && uint.TryParse(args[1].Trim(), out var a) ? a : 0;
            uint index = args.Length > 2 && uint.TryParse(args[2].Trim(), out var i) ? i : 0;
            return wallet.GetPublicKey(account, index, testnet);
        };

        // Timestamp.Now - returns current unix timestamp
        _macros["Timestamp.Now"] = _ => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Timestamp.NowString - returns "now" for Bitcoin Core timestamp parameter
        _macros["Timestamp.NowString"] = _ => "now";
    }

    /// <summary>
    /// Register a custom macro function
    /// </summary>
    public void RegisterMacro(string name, Func<string[], string> handler)
    {
        _macros[name] = handler;
    }

    /// <summary>
    /// Process a command template by replacing all placeholders
    /// </summary>
    /// <param name="command">The custom command definition</param>
    /// <param name="userInputs">Dictionary of user-provided input values</param>
    /// <returns>The fully resolved command string</returns>
    public string ProcessCommand(CustomCommand command, Dictionary<string, string> userInputs)
    {
        var parts = new List<string> { command.Binary };

        if (command.Arguments != null)
        {
            foreach (var arg in command.Arguments)
            {
                var processed = ProcessTemplate(arg, userInputs);
                parts.Add(processed);
            }
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Process a single template string
    /// </summary>
    public string ProcessTemplate(string template, Dictionary<string, string> userInputs)
    {
        var result = template;

        // Process input placeholders first
        result = InputPattern.Replace(result, match =>
        {
            var inputName = match.Groups[1].Value;
            if (userInputs.TryGetValue(inputName, out var value))
            {
                return value;
            }
            throw new InvalidOperationException(string.Format(Strings.CustomActions.MissingInputFormat, inputName));
        });

        // Process macro placeholders
        result = MacroPattern.Replace(result, match =>
        {
            var macroName = match.Groups[1].Value;
            var argsStr = match.Groups[2].Value;
            var args = string.IsNullOrWhiteSpace(argsStr) 
                ? Array.Empty<string>() 
                : argsStr.Split(',').Select(a => a.Trim()).ToArray();

            if (_macros.TryGetValue(macroName, out var handler))
            {
                return handler(args);
            }
            throw new InvalidOperationException(string.Format(Strings.CustomActions.UnknownMacroFormat, macroName));
        });

        return result;
    }

    /// <summary>
    /// Collect user inputs for a command based on its input definitions
    /// </summary>
    public Dictionary<string, string> CollectUserInputs(CustomCommand command)
    {
        var inputs = new Dictionary<string, string>();

        if (command.Inputs == null || command.Inputs.Count == 0)
        {
            return inputs;
        }

        foreach (var input in command.Inputs)
        {
            string value;

            if (input.Type.ToLower() == "password")
            {
                var prompt = new TextPrompt<string>(input.Prompt).Secret();
                if (!input.Required)
                {
                    prompt.AllowEmpty();
                }
                value = AnsiConsole.Prompt(prompt);
            }
            else if (input.Type.ToLower() == "bool")
            {
                var defaultBool = input.Default?.ToLower() == "true";
                var confirm = AnsiConsole.Confirm(input.Prompt, defaultBool);
                value = confirm.ToString().ToLower();
            }
            else
            {
                var prompt = new TextPrompt<string>(input.Prompt);
                if (!input.Required)
                {
                    prompt.AllowEmpty();
                }
                
                if (!string.IsNullOrEmpty(input.Default))
                {
                    prompt.DefaultValue(input.Default);
                }

                value = AnsiConsole.Prompt(prompt);
            }

            // Validate if pattern is specified (with timeout to prevent ReDoS attacks)
            if (!string.IsNullOrEmpty(input.Pattern) && !string.IsNullOrEmpty(value))
            {
                try
                {
                    var regex = new Regex(input.Pattern, RegexOptions.None, RegexTimeout);
                    while (!regex.IsMatch(value))
                    {
                        AnsiConsole.MarkupLine(string.Format(Strings.CustomActions.InvalidFormatFormat, input.Pattern));
                        value = AnsiConsole.Prompt(new TextPrompt<string>(input.Prompt));
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Pattern took too long - likely a malicious pattern, skip validation
                    AnsiConsole.MarkupLine("[yellow]Pattern validation skipped due to complexity[/]");
                }
            }

            inputs[input.Name] = value ?? "";
        }

        return inputs;
    }

    /// <summary>
    /// Get list of available macros for documentation
    /// </summary>
    public IEnumerable<string> GetAvailableMacros() => _macros.Keys;
}
