using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Arcana.App.Services;

public sealed class RecentItem
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public IRelayCommand? OpenCommand { get; set; }
}

/// <summary>
/// Most-recently-opened archives. Persisted to %APPDATA%\Arcana\recent.json,
/// newest first, capped at <see cref="MaxEntries"/>. Only paths that still exist
/// are kept when reloaded.
/// </summary>
public sealed class RecentArchivesService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<RecentArchivesService>();

    public const int MaxEntries = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;
    private readonly List<RecentItem> _items = [];

    public event EventHandler? Changed;

    public IReadOnlyList<RecentItem> Items => _items;
    public bool IsEmpty => _items.Count == 0;

    public RecentArchivesService()
    {
        var dir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Arcana");
        _filePath = Path.Combine(dir, "recent.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;
            var json = File.ReadAllText(_filePath);
            var stored = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            if (stored == null)
                return;

            _items.Clear();
            foreach (var path in stored)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;
                _items.Add(new RecentItem { Name = Path.GetFileName(path), Path = path });
            }
            Log.Debug("Loaded {Count} recent archives from {File}", _items.Count, _filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Recent file {File} is corrupted; falling back to empty", _filePath);
        }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var stored = _items.Select(i => i.Path).ToList();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(stored, JsonOptions));
            Log.Debug("Persisted {Count} recent archives to {File}", _items.Count, _filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not persist recent archives to {File}", _filePath);
        }
    }

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        _items.RemoveAll(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, new RecentItem { Name = Path.GetFileName(path), Path = path });
        while (_items.Count > MaxEntries)
            _items.RemoveAt(_items.Count - 1);
        Log.Information("Recorded recent archive {Path}", path);
        Persist();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(RecentItem item)
    {
        _items.Remove(item);
        Log.Information("Removed recent archive {Path}", item.Path);
        Persist();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_items.Count == 0)
            return;
        _items.Clear();
        Log.Information("Cleared recent archives list");
        Persist();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RebindCommands(Func<RecentItem, IRelayCommand> factory)
    {
        foreach (var item in _items)
            item.OpenCommand = factory(item);
    }
}
