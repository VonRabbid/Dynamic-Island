using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DynamicIsland.Models;

namespace DynamicIsland.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DynamicIsland");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static SettingsService Instance { get; } = new();

    public AppSettings CurrentSettings { get; private set; }

    public event Action<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        CurrentSettings = Load();
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Error al cargar configuración: {ex.Message}");
        }

        var defaultSettings = new AppSettings();
        Save(defaultSettings);
        return defaultSettings;
    }

    public void Save(AppSettings? settings = null)
    {
        if (settings != null)
        {
            CurrentSettings = settings;
        }

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(CurrentSettings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
            SettingsChanged?.Invoke(CurrentSettings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Error al guardar configuración: {ex.Message}");
        }
    }

    public void NotifyChanged()
    {
        SettingsChanged?.Invoke(CurrentSettings);
    }
}
