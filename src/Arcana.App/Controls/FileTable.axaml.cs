using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Arcana.App.ViewModels;
using Arcana.Core.Filesystem;
using Arcana.Core.Compression;
using Serilog;

namespace Arcana.App.Controls;

public partial class FileTable : UserControl
{
    private static readonly ILogger Log = Serilog.Log.ForContext<FileTable>();

    private PointerPressedEventArgs? _pressArgs;
    private ArchiveNode? _pressNode;
    private Point _pressPoint;
    private bool _dragging;

    public FileTable()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Grid.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Grid.AddHandler(InputElement.PointerPressedEvent, OnGridPointerPressed,
            RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        Grid.AddHandler(InputElement.PointerMovedEvent, OnGridPointerMoved,
            RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        Grid.AddHandler(InputElement.PointerReleasedEvent, OnGridPointerReleased,
            RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (Vm is { Archive: { } archive })
        {
            archive.SelectAllRequested -= OnSelectAllRequested;
            archive.SelectAllRequested += OnSelectAllRequested;
        }
    }

    private void OnSelectAllRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Grid.SelectAll();
            SyncSelectionToVm();
        });
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        Vm?.OpenSelectedCommand.Execute(null);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SyncSelectionToVm();
    }

    private void SyncSelectionToVm()
    {
        if (Vm?.Archive is not { } archive)
            return;
        archive.SelectedItems = Grid.SelectedItems.Cast<FileEntryItem>().ToList();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                Vm?.InfoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                Vm?.OpenSelectedCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Back:
            case Key.Up when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                Vm?.Archive.NavigateUpCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete:
                Vm?.Archive.DeleteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F2:
                Vm?.RenameCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                Grid.SelectAll();
                SyncSelectionToVm();
                e.Handled = true;
                break;
        }
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(Grid).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            return;
        if (Vm is not { Archive.Archive: { } } vm)
            return;

        var node = FindNodeAt(e);
        if (node == null || node == Vm?.Archive.CurrentNode?.Parent)
            return;

        _pressArgs = e;
        _pressNode = node;
        _pressPoint = e.GetCurrentPoint(Grid).Position;
    }

    private async void OnGridPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressNode == null || _dragging)
            return;
        if (!e.GetCurrentPoint(Grid).Properties.IsLeftButtonPressed)
        {
            _pressNode = null;
            return;
        }

        var pos = e.GetCurrentPoint(Grid).Position;
        var dx = pos.X - _pressPoint.X;
        var dy = pos.Y - _pressPoint.Y;
        if (dx * dx + dy * dy < 16)
            return;

        var args = _pressArgs;
        var node = _pressNode;
        _pressNode = null;
        if (args == null)
            return;

        _dragging = true;
        try
        {
            await DragOutAsync(args, node);
        }
        finally
        {
            _dragging = false;
            _pressArgs = null;
        }
    }

    private void OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pressNode = null;
    }

    private async Task DragOutAsync(PointerPressedEventArgs args, ArchiveNode pressedNode)
    {
        var vm = Vm;
        if (vm?.Archive is not { } archive || archive.Archive == null)
            return;

        var nodes = archive.SelectedNodes.Contains(pressedNode)
            ? archive.SelectedNodes
            : new[] { pressedNode };
        var files = nodes.ToList();
        if (files.Count == 0)
            return;

        var tempDir = Path.Combine(Path.GetTempPath(), "ArcanaDrag", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            await ArchiveExtractor.ExtractAsync(files, tempDir);

            var storageProvider = TopLevel.GetTopLevel(Grid)?.StorageProvider;
            if (storageProvider == null)
                return;

            var transfer = new DataTransfer();
            foreach (var file in files)
            {
                var path = ArchiveExtractor.GetDestinationPath(tempDir, file.FullPath);
                IStorageItem? storageFile = file.Type == NodeType.Directory
                    ? await storageProvider.TryGetFolderFromPathAsync(new Uri(path))
                    : await storageProvider.TryGetFileFromPathAsync(new Uri(path));
                if (storageFile != null)
                    transfer.Add(DataTransferItem.CreateFile(storageFile));
            }
            if (transfer.Items.Count == 0)
                return;

            await DragDrop.DoDragDropAsync(args, transfer, DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Drag-out of {Count} file(s) failed", files.Count);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { Log.Warning(ex, "Could not clean drag temporary {Path}", tempDir); }
        }
    }

    private ArchiveNode? FindNodeAt(PointerPressedEventArgs e)
    {
        var visual = e.Source as Visual;
        while (visual != null)
        {
            if (visual is DataGridRow { DataContext: FileEntryItem item })
                return item.Node;
            visual = visual.Parent as Visual;
        }
        return null;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.Archive.Archive == null)
            return;
        if (!e.DataTransfer.Formats.Contains(DataFormat.File))
            return;

        var files = Avalonia.Input.DataTransferExtensions.TryGetFiles(e.DataTransfer);
        if (files is not { Length: > 0 })
            return;

        var paths = files
            .Select(f => f.TryGetLocalPath() ?? f.Path.LocalPath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        if (paths.Count == 0)
            return;

        Log.Debug("Dropping {Count} file(s) into archive", paths.Count);
        _ = vm.AddFilesToArchiveAsync(paths);
    }
}
