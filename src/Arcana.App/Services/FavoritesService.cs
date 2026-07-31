using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Arcana.App.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.Services;

public sealed class FavoriteItem
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public IRelayCommand? OpenCommand { get; set; }
}

public sealed class FavoritesService
{
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
        }
        catch (Exception)
        {
            // corrupted favorites fall back to empty
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
        }
        catch (Exception)
        {
            // persistence is best-effort
        }
    }

    public void Add(string name, string path)
    {
        _items.RemoveAll(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, new FavoriteItem { Name = name, Path = path });
        Persist();
    }

    public void Remove(FavoriteItem item)
    {
        _items.Remove(item);
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
