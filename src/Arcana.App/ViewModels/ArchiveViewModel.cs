using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Arcana.App.Services;
using Arcana.Core.Compression;
using Arcana.Core.Filesystem;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

public partial class ArchiveViewModel : ObservableObject
{
    private readonly Stack<string> _history = new();

    public event EventHandler<FileEntryItem?>? SelectionChanged;
    public event EventHandler? ContentsChanged;
    public event EventHandler? SelectAllRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NavigateUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private ArchiveNode? _root;

    [ObservableProperty]
    private ObservableCollection<ArchiveNode> _treeNodes = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NavigateUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private ArchiveNode? _currentNode;

    [ObservableProperty]
    private ObservableCollection<FileEntryItem> _entries = [];

    [ObservableProperty]
    private FileEntryItem? _selectedItem;

    [ObservableProperty]
    private IList<FileEntryItem> _selectedItems = new List<FileEntryItem>();

    [ObservableProperty]
    private string _filter = "";

    [ObservableProperty]
    private string _breadcrumbText = "/";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyText = "";

    private ObservableCollection<FileEntryItem> _fullEntries = [];
    private Archive? _archive;

    public bool HasArchive => Root != null;
    public Archive? Archive => _archive;

    partial void OnSelectedItemChanged(FileEntryItem? value)
        => SelectionChanged?.Invoke(this, value);

    partial void OnFilterChanged(string value)
        => ApplyFilter();

    partial void OnCurrentNodeChanged(ArchiveNode? value)
    {
        if (value == null)
            return;
        BuildEntries();
        BuildBreadcrumb();
    }

    public void LoadArchive(Archive archive, string displayName)
    {
        _archive?.Dispose();
        _archive = archive;
        archive.SyncNodeMetadata();

        var vfsRoot = archive.Vfs.Root;
        vfsRoot.Name = displayName;
        _history.Clear();
        Root = vfsRoot;
        TreeNodes.Clear();
        TreeNodes.Add(vfsRoot);
        CurrentNode = vfsRoot;
        Filter = "";
    }

    public void Close()
    {
        _archive?.Dispose();
        _archive = null;
        Root = null;
        CurrentNode = null;
        Entries.Clear();
        _fullEntries.Clear();
        TreeNodes.Clear();
        BreadcrumbText = "/";
        SelectedItem = null;
        SelectedItems = new List<FileEntryItem>();
        ContentsChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanOpenItem))]
    private void OpenItem()
    {
        if (SelectedItem is not { IsDirectory: true } item)
            return;
        NavigateTo(item.Node);
    }

    private bool CanOpenItem() => SelectedItem is { IsDirectory: true };

    [RelayCommand(CanExecute = nameof(CanNavigateUp))]
    private void NavigateUp()
    {
        if (CurrentNode?.Parent is { } parent)
            NavigateTo(parent);
    }

    private bool CanNavigateUp() => CurrentNode?.Parent != null;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (_history.Count == 0)
            return;
        var target = _history.Pop();
        if (FindNode(target) is { } node)
            CurrentNode = node;
    }

    private bool CanGoBack() => _history.Count > 0 && Root != null;

    [RelayCommand]
    private void SelectAll()
    {
        SelectAllRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedItems = new List<FileEntryItem>();
    }

    public IReadOnlyList<ArchiveNode> SelectedNodes
    {
        get
        {
            if (SelectedItems is { Count: > 0 } list)
                return list.Select(i => i.Node).ToList();
            return SelectedItem != null
                ? new[] { SelectedItem.Node }
                : Array.Empty<ArchiveNode>();
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        var nodes = SelectedNodes;
        if (nodes.Count == 0)
            return;

        foreach (var node in nodes)
        {
            node.Parent?.Children.Remove(node);
            node.Parent = null;
        }
        SelectedItems = new List<FileEntryItem>();
        RefreshCurrent();
        ContentsChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool CanDelete() => SelectedItem != null;

    public void NavigateTo(ArchiveNode node)
    {
        if (Root == null || node == CurrentNode)
            return;

        if (CurrentNode != null)
            _history.Push(CurrentNode.FullPath);

        CurrentNode = node;
        TreeNodes.Clear();
        TreeNodes.Add(Root);
    }

    public long TotalSizeOfCurrent => _fullEntries.Where(e => !e.IsDirectory).Sum(e => e.Node.OriginalSize);

    public string BuildStatusText()
    {
        var files = _fullEntries.Count(e => !e.IsDirectory);
        var dirs = _fullEntries.Count - files;
        var total = TotalSizeOfCurrent;

        var text = $"{files} files · {ByteFormat.Format(total)}";
        if (dirs > 0)
            text += $" · {dirs} folders";

        if (SelectedItem is { } sel)
        {
            text += $" · 1 sel ({ByteFormat.Format(sel.Node.OriginalSize)})";
        }
        return text;
    }

    public void RefreshCurrent()
    {
        if (CurrentNode != null)
            BuildEntries();
        SelectedItems = new List<FileEntryItem>();
    }

    private void BuildEntries()
    {
        var items = CurrentNode!.Children
            .OrderBy(c => c.Type == NodeType.Directory ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new FileEntryItem(c))
            .ToList();

        _fullEntries = new ObservableCollection<FileEntryItem>(items);
        Entries = new ObservableCollection<FileEntryItem>(_fullEntries);
        ApplyFilter();
        ContentsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(Filter))
        {
            Entries = _fullEntries;
            return;
        }

        Entries = new ObservableCollection<FileEntryItem>(
            _fullEntries.Where(e =>
                e.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)));
    }

    private void BuildBreadcrumb()
    {
        var parts = new List<string>();
        ArchiveNode? node = CurrentNode;
        while (node != null && node != Root)
        {
            parts.Insert(0, node.Name);
            node = node.Parent;
        }
        BreadcrumbText = "/" + string.Join("/", parts);
    }

    private ArchiveNode? FindNode(string fullPath)
    {
        if (Root == null)
            return null;
        return Root.FullPath == fullPath ? Root : FindRecursive(Root, fullPath);
    }

    private static ArchiveNode? FindRecursive(ArchiveNode node, string fullPath)
    {
        foreach (var child in node.Children)
        {
            if (child.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                return child;
            if (child.Type == NodeType.Directory)
            {
                var found = FindRecursive(child, fullPath);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    public IEnumerable<ArchiveNode> CollectSelectedForDelete() =>
        SelectedNodes;
}
