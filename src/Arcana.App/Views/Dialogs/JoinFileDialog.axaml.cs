using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels;
using Arcana.Core.Tools;

namespace Arcana.App.Views.Dialogs;

public partial class JoinFileDialog : Window
{
    public JoinFileDialog()
    {
        InitializeComponent();
    }

    private async void OnBrowseFirstPartClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not JoinFileViewModel vm)
            return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select first part (.001)",
            AllowMultiple = false,
        });

        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (path == null)
            return;

        try
        {
            var parts = FileJoiner.AutoDiscoverParts(path);
            vm.FirstPart = path;
            vm.PartCount = parts.Count;
            vm.OutputPath = Path.Combine(
                Path.GetDirectoryName(path) ?? "",
                Path.GetFileNameWithoutExtension(path) + ".joined");
        }
        catch
        {
            vm.FirstPart = path;
            vm.PartCount = 0;
        }
    }

    private async void OnBrowseOutputClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not JoinFileViewModel vm)
            return;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select output file",
            SuggestedFileName = string.IsNullOrEmpty(vm.OutputPath)
                ? "joined.bin"
                : Path.GetFileName(vm.OutputPath),
        });

        if (file != null)
            vm.OutputPath = file.TryGetLocalPath() ?? vm.OutputPath;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is JoinFileViewModel vm && vm.FirstPart.Length > 0 && vm.OutputPath.Length > 0)
            vm.Confirm();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
