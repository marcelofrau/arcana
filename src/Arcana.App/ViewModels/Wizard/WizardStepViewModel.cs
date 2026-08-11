using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels.Wizard;

/// <summary>
/// A single step in a nostalgic archive-manager wizard. Steps are pure UI input
/// collectors; execution happens in the caller after the wizard closes.
/// </summary>
public abstract class WizardStepViewModel : ObservableObject
{
    public abstract string Title { get; }

    public abstract string Description { get; }

    /// <summary>Whether "Next"/"Finish" may be pressed for the current input state.</summary>
    public virtual bool CanProceed => true;

    /// <summary>Raised whenever inputs change so the host can re-evaluate <see cref="CanProceed"/>.</summary>
    public event EventHandler? StateChanged;

    public virtual void OnEnter()
    {
    }

    protected void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
