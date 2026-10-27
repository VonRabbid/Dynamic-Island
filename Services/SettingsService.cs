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
                using var fs = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(fs);
                string json = reader.ReadToEnd();

                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return SanitizeAndValidate(settings);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Advertencia de seguridad o archivo de configuración corrupto: {ex.Message}");
        }

        var defaultSettings = new AppSettings();
        Save(defaultSettings);
        return defaultSettings;
    }

    public void Save(AppSettings? settings = null)
    {
        CurrentSettings = SanitizeAndValidate(settings ?? CurrentSettings);

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(CurrentSettings, JsonOptions);

            using var fs = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fs);
            writer.Write(json);
            writer.Flush();

            SettingsChanged?.Invoke(CurrentSettings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Error al guardar configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Validación defensiva de límites y tipos para evitar inyecciones o estados inválidos.
    /// </summary>
    public static AppSettings SanitizeAndValidate(AppSettings? settings)
    {
        if (settings == null)
            return new AppSettings();

        // 1. Limitar VerticalOffset entre 0 y 200 px
        if (double.IsNaN(settings.VerticalOffset) || double.IsInfinity(settings.VerticalOffset) || settings.VerticalOffset < 0.0 || settings.VerticalOffset > 200.0)
        {
            settings.VerticalOffset = Math.Clamp(double.IsNaN(settings.VerticalOffset) ? 0.0 : settings.VerticalOffset, 0.0, 200.0);
        }

        // 2. Limitar NotificationDuration entre 1 y 15 segundos
        if (settings.NotificationDuration < 1 || settings.NotificationDuration > 15)
        {
            settings.NotificationDuration = Math.Clamp(settings.NotificationDuration, 1, 15);
        }

        // 3. Limitar IslandScale entre 0.5 y 1.5
        if (double.IsNaN(settings.IslandScale) || double.IsInfinity(settings.IslandScale) || settings.IslandScale < 0.5 || settings.IslandScale > 1.5)
        {
            settings.IslandScale = Math.Clamp(double.IsNaN(settings.IslandScale) ? 1.0 : settings.IslandScale, 0.5, 1.5);
        }

        // 4. Limitar IslandOpacity entre 0.40 y 1.00 (por defecto 0.85)
        if (double.IsNaN(settings.IslandOpacity) || double.IsInfinity(settings.IslandOpacity) || settings.IslandOpacity < 0.40 || settings.IslandOpacity > 1.00)
        {
            settings.IslandOpacity = (double.IsNaN(settings.IslandOpacity) || double.IsInfinity(settings.IslandOpacity))
                ? 0.85
                : Math.Clamp(settings.IslandOpacity, 0.40, 1.00);
        }

        // 5. Sanitizar SelectedMonitorDeviceName
        if (string.IsNullOrWhiteSpace(settings.SelectedMonitorDeviceName) || settings.SelectedMonitorDeviceName.Length > 128)
        {
            settings.SelectedMonitorDeviceName = "Primary";
        }

        // 6. Validar coordenadas de posición personalizada
        if (double.IsNaN(settings.CustomPositionX) || double.IsInfinity(settings.CustomPositionX))
        {
            settings.CustomPositionX = -1;
            settings.UseCustomPosition = false;
        }
        if (double.IsNaN(settings.CustomPositionY) || double.IsInfinity(settings.CustomPositionY))
        {
            settings.CustomPositionY = -1;
            settings.UseCustomPosition = false;
        }

        return settings;
    }

    public void SaveSettings(AppSettings? settings = null) => Save(settings);

    public void NotifyChanged()
    {
        CurrentSettings = SanitizeAndValidate(CurrentSettings);
        SettingsChanged?.Invoke(CurrentSettings);
    }
}
