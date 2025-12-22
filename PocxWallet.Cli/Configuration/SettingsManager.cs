namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Utility class for loading and managing application settings
/// </summary>
public static class SettingsManager
{
    private const string DefaultSettingsPath = "appsettings.json";

    /// <summary>
    /// Load settings from the default appsettings.json file
    /// </summary>
    public static AppSettings LoadSettings()
    {
        return LoadSettings(DefaultSettingsPath);
    }

    /// <summary>
    /// Load settings from a specified path
    /// </summary>
    public static AppSettings LoadSettings(string settingsPath)
    {
        var settings = new AppSettings();
        
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (config != null)
                {
                    settings = config;
                }
            }
            catch
            {
                // Use defaults if loading fails
            }
        }
        
        return settings;
    }

    /// <summary>
    /// Save settings to the default appsettings.json file
    /// </summary>
    public static void SaveSettings(AppSettings settings)
    {
        SaveSettings(settings, DefaultSettingsPath);
    }

    /// <summary>
    /// Save settings to a specified path
    /// </summary>
    public static void SaveSettings(AppSettings settings, string settingsPath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(settingsPath, json);
    }
}
