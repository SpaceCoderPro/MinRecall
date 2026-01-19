using MinRecall.Core.Models;
using System.Text.Json;

namespace MinRecall.Core.Settings;

public class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MinRecall",
        "settings.json"
    );

    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public SettingsManager()
    {
        Load();
    }

    public void Load()
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                _settings = new AppSettings();
            }
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(SettingsPath, json);
    }

    public void UpdateQualityProfile(ImageQualityProfile profile)
    {
        _settings.QualityProfile = profile;

        switch (profile)
        {
            case ImageQualityProfile.Storage:
                _settings.JpegQuality = 70;
                _settings.KeyframeFrequency = 30;
                _settings.TargetWidth = 1024;
                _settings.TargetHeight = 576;
                break;
            case ImageQualityProfile.Balanced:
                _settings.JpegQuality = 85;
                _settings.KeyframeFrequency = 25;
                _settings.TargetWidth = 1280;
                _settings.TargetHeight = 720;
                break;
            case ImageQualityProfile.Quality:
                _settings.JpegQuality = 95;
                _settings.KeyframeFrequency = 15;
                _settings.TargetWidth = 1920;
                _settings.TargetHeight = 1080;
                break;
        }

        Save();
    }

    public void ApplyProfileToSettings()
    {
        UpdateQualityProfile(_settings.QualityProfile);
    }
}
