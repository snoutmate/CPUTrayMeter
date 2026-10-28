using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CpuTrayMeter;

// save and load settings from file in AppData
public sealed class SettingsService
{
    private readonly string mFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService(string appName)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName
        );

        mFilePath = Path.Combine(folder, "settings.json");

        Directory.CreateDirectory(folder);

        Trace.WriteLine($"{mFilePath}");
    }

    public AppSettings Load()
    {
        if (!File.Exists(mFilePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(mFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (JsonException) // invalid JSON
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var settingsJson = JsonSerializer.Serialize(settings, JsonOptions);

        // write to temporary file first, so settings file won't get corrupted in case writing fails
        var tempPath = mFilePath + ".tmp";
        File.WriteAllText(tempPath, settingsJson);
        File.Move(tempPath, mFilePath, overwrite: true);

        Trace.WriteLine($"Settings saved to {mFilePath}");
    }
}