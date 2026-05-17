using System.Collections.Generic;
using System.Text;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Utility for handling command line arguments and escaping
/// </summary>
public static class CommandLineHelper
{
    /// <summary>
    /// Split command line arguments while respecting quoted strings
    /// This handles cases like: bitcoin-cli importdescriptors '[{"desc": "...", "timestamp": "now"}]'
    /// Also handles escaped quotes with backslash
    /// </summary>
    public static string[] SplitCommandLineArguments(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return System.Array.Empty<string>();

        var args = new List<string>();
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var current = new StringBuilder();
        var escapeNext = false;

        foreach (char c in commandLine)
        {
            if (escapeNext)
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
            }
            else if (c == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
            }
            else if (c == ' ' && !inSingleQuote && !inDoubleQuote)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        return args.ToArray();
    }
}
