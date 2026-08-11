using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Arcana.App.Services;
using Arcana.App.ViewModels.Wizard;

namespace Arcana.App.Views.Wizards;

public partial class WizardWindow : Window
{
    public WizardWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows a wizard modally. Returns the wizard result, or <c>default</c> if the
    /// user cancelled or closed the window. Execution of the operation itself happens
    /// in the caller (typically behind the modal progress dialog).
    /// </summary>
    public static async Task<TResult?> ShowAsync<TResult>(Window owner, WizardViewModel<TResult> vm)
    {
        var window = new WizardWindow { DataContext = vm };
        object? result = null;
        vm.Finished += r => { result = r; window.Close(); };
        vm.Cancelled += () => window.Close();

        await ModalHost.ShowAsync(owner, window);
        return result is TResult t ? t : default;
    }
}
