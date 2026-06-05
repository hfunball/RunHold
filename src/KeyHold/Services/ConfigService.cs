using System.IO;
using System.Text.Json;
using KeyHold.Models;

namespace KeyHold.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeyHold",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var folder = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
    }
}

