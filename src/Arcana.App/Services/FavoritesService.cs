using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Arcana.App.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Arcana.App.Services;

public sealed class FavoriteItem
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public IRelayCommand? OpenCommand { get; set; }
}

public sealed class FavoritesService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<FavoritesService>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;
    private readonly List<FavoriteItem> _items = [];

    public IReadOnlyList<FavoriteItem> Items => _items;

    public FavoritesService()
    {
        var dir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Arcana");
        _filePath = Path.Combine(dir, "favorites.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;
            var json = File.ReadAllText(_filePath);
            var stored = JsonSerializer.Deserialize<List<StoredFavorite>>(json, JsonOptions);
            if (stored == null)
                return;
            _items.Clear();
            foreach (var f in stored)
                _items.Add(new FavoriteItem { Name = f.Name, Path = f.Path });
            Log.Debug("Loaded {Count} favorites from {File}", _items.Count, _filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Favorites file {File} is corrupted; falling back to empty", _filePath);
        }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var stored = _items.Select(f => new StoredFavorite { Name = f.Name, Path = f.Path }).ToList();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(stored, JsonOptions));
            Log.Debug("Persisted {Count} favorites to {File}", _items.Count, _filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not persist favorites to {File}", _filePath);
        }
    }

    public void Add(string name, string path)
    {
        _items.RemoveAll(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, new FavoriteItem { Name = name, Path = path });
        Log.Information("Added favorite {Name} ({Path})", name, path);
        Persist();
    }

    public void Remove(FavoriteItem item)
    {
        _items.Remove(item);
        Log.Information("Removed favorite {Name} ({Path})", item.Name, item.Path);
        Persist();
    }

    public void RebindCommands(Func<FavoriteItem, IRelayCommand> factory)
    {
        foreach (var item in _items)
            item.OpenCommand = factory(item);
    }

    private sealed class StoredFavorite
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
