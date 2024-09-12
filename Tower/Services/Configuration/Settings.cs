using System.Text.Json;

namespace Tower.Services.Configuration;
public class Settings
{
    public string? Token { get; init; }
    private const string ConfigFileName = "config.json";

    public static Settings Load()
    {
        try
        {
            if (!File.Exists(ConfigFileName))
            {
                throw new FileNotFoundException($"Configuration file '{ConfigFileName}' not found.");
            }
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigFileName))
            ?? throw new Exception("Failed to deserialize configuration file.");

            if (string.IsNullOrEmpty(settings.Token))
            {
                throw new Exception($"Token not specified in '{ConfigFileName}'");
            }
            return settings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            throw;
        }
    }
}