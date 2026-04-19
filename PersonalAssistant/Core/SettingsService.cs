using System.IO;
using System.Text.Json;
using PersonalAssistant.Models;

namespace PersonalAssistant.Core;

public class SettingsService
{
    private static readonly string SettingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PersonalAssistant", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (System.IO.File.Exists(SettingsPath))
            {
                var json = System.IO.File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            System.IO.File.WriteAllText(SettingsPath, json);
        }
        catch
        {
        }
    }
}
