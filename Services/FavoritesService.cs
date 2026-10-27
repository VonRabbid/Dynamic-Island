using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using DynamicIsland.Models;

namespace DynamicIsland.Services;

public class FavoritesService
{
    private static readonly string StorageDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DynamicIsland");

    private static readonly string FilePath = Path.Combine(StorageDirectory, "favorites.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly List<FavoriteTrack> _favorites = new();
    private readonly object _lock = new();

    public static FavoritesService Instance { get; } = new();

    public event EventHandler? FavoritesChanged;

    public FavoritesService()
    {
        Load();
    }

    public IReadOnlyList<FavoriteTrack> GetAll()
    {
        lock (_lock)
        {
            return _favorites.ToList();
        }
    }

    public bool IsFavorite(string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        string cleanTitle = title.Trim();
        string cleanArtist = (artist ?? string.Empty).Trim();

        lock (_lock)
        {
            return _favorites.Any(f =>
                string.Equals(f.Title.Trim(), cleanTitle, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Artist.Trim(), cleanArtist, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool ToggleFavorite(string title, string artist, string? appSource = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        string cleanTitle = title.Trim();
        string cleanArtist = (artist ?? string.Empty).Trim();
        bool isNowFav;

        lock (_lock)
        {
            var existing = _favorites.FirstOrDefault(f =>
                string.Equals(f.Title.Trim(), cleanTitle, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Artist.Trim(), cleanArtist, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _favorites.Remove(existing);
                isNowFav = false;
            }
            else
            {
                _favorites.Add(new FavoriteTrack
                {
                    Title = cleanTitle,
                    Artist = cleanArtist,
                    AppSource = appSource ?? string.Empty,
                    AddedAt = DateTime.UtcNow
                });
                isNowFav = true;
            }

            Save();
        }

        FavoritesChanged?.Invoke(this, EventArgs.Empty);
        return isNowFav;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var list = JsonSerializer.Deserialize<List<FavoriteTrack>>(json, JsonOptions);
                if (list != null)
                {
                    lock (_lock)
                    {
                        _favorites.Clear();
                        _favorites.AddRange(list);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FavoritesService] Error cargando favoritos: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(StorageDirectory);
            string json = JsonSerializer.Serialize(_favorites, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FavoritesService] Error guardando favoritos: {ex.Message}");
        }
    }
}
