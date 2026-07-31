using System;
using System.Collections;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Arcana.Core.Filesystem;
using Arcana.App.ViewModels;

namespace Arcana.App.Controls;

public partial class FolderTree : UserControl
{
    public FolderTree()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Archive.PropertyChanged -= OnArchivePropertyChanged;
            vm.Archive.PropertyChanged += OnArchivePropertyChanged;
            Dispatcher.UIThread.Post(ExpandAll);
        }
    }

    private void OnArchivePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ArchiveViewModel.Root))
            Dispatcher.UIThread.Post(ExpandAll);
    }

    private void ExpandAll()
    {
        if (Tree.ItemsSource is not IEnumerable items)
            return;

        int index = 0;
        foreach (var item in items)
        {
            var container = Tree.ContainerFromIndex(index);
            ExpandItem(container as TreeViewItem);
            index++;
        }
    }

    private static void ExpandItem(TreeViewItem? container)
    {
        if (container == null)
            return;
        container.IsExpanded = true;
        for (int i = 0; i < container.ItemCount; i++)
        {
            var child = container.ContainerFromIndex(i);
            ExpandItem(child as TreeViewItem);
        }
    }
}
