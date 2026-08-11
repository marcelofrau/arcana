using Arcana.App.Icons;
using Arcana.App.Services;
using Arcana.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Arcana.App.Views.Dialogs;

public partial class ProgressDialog : Window
{
    public ProgressDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the dialog, runs <paramref name="work"/> with a progress reporter wired to
    /// <see cref="ProgressViewModel.Update"/>, then closes. The dialog is modal for the
    /// given owner. Cancellation is surfaced through the VM's token source.
    /// </summary>
    public static async Task ShowAsync(
        Window owner,
        string title,
        bool showChart,
        long totalOriginalBytes,
        Func<IProgress<Arcana.Core.Compression.ProgressReport>, CancellationToken, Task> work,
        IconKey headerIcon = IconKey.FileArchive)
    {
        var vm = new ProgressViewModel(title, showChart, totalOriginalBytes, headerIcon);
        var dialog = new ProgressDialog { DataContext = vm };
        WindowFade.Attach(dialog);
        var progress = new Progress<Arcana.Core.Compression.ProgressReport>(vm.Update);
        var overlay = ModalHost.Attach(owner);
        var shown = dialog.ShowDialog(owner);
        try
        {
            await work(progress, vm.CancelSource.Token);
            vm.Complete();
        }
        finally
        {
            dialog.Close();
            await shown;
            ModalHost.Detach(owner, overlay);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProgressViewModel vm)
            vm.CancelSource.Cancel();
    }
}
