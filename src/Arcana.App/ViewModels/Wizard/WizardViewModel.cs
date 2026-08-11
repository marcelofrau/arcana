using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels.Wizard;

/// <summary>
/// Generic host for multi-step wizards. Steps are exposed via <see cref="Current"/> and
/// rendered by the wizard window through DataTemplates keyed on the step type.
/// Navigation (Back/Next/Finish/Cancel) is driven here; the window only forwards the
/// finished result.
/// </summary>
public abstract partial class WizardViewModel<TResult> : ObservableObject
{
    protected WizardViewModel()
    {
        Steps = new ObservableCollection<WizardStepViewModel>(BuildSteps());
        foreach (var step in Steps)
            step.StateChanged += (_, _) => NotifyNavStateChanged();
        if (Steps.Count > 0)
            Steps[0].OnEnter();
    }

    public IReadOnlyList<WizardStepViewModel> Steps { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Current))]
    [NotifyPropertyChangedFor(nameof(IsFirst))]
    [NotifyPropertyChangedFor(nameof(IsLast))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    [NotifyPropertyChangedFor(nameof(CanForward))]
    [NotifyPropertyChangedFor(nameof(ForwardLabel))]
    [NotifyPropertyChangedFor(nameof(IsBackEnabled))]
    [NotifyPropertyChangedFor(nameof(StepIndicatorText))]
    private int _currentIndex;

    public abstract string Title { get; }

    public WizardStepViewModel Current => Steps[CurrentIndex];

    public bool IsFirst => CurrentIndex == 0;

    public bool IsLast => CurrentIndex == Steps.Count - 1;

    public bool CanGoNext => !IsLast && Current.CanProceed;

    public bool CanFinish => IsLast && Current.CanProceed;

    public bool CanForward => Current.CanProceed;

    public string ForwardLabel => IsLast
        ? Localization.LocalizationManager.T("wizard.finish")
        : Localization.LocalizationManager.T("wizard.next");

    public bool IsBackEnabled => CurrentIndex > 0;

    public string StepIndicatorText
        => Localization.LocalizationManager.T("wizard.stepOf", CurrentIndex + 1, Steps.Count);

    public event Action<TResult>? Finished;
    public event Action? Cancelled;

    /// <summary>Raised when any step reports a state change, so nav re-evaluates.</summary>
    public event EventHandler? NavigationChanged;

    [RelayCommand]
    public void Next()
    {
        if (CurrentIndex >= Steps.Count - 1)
            return;
        CurrentIndex++;
        Current.OnEnter();
    }

    [RelayCommand]
    public void Back()
    {
        if (CurrentIndex <= 0)
            return;
        CurrentIndex--;
    }

    [RelayCommand]
    public void Forward()
    {
        if (IsLast)
            Finish();
        else
            Next();
    }

    [RelayCommand]
    public void Finish() => Finished?.Invoke(BuildResult());

    [RelayCommand]
    public void Cancel() => Cancelled?.Invoke();

    private void NotifyNavStateChanged()
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanFinish));
        NavigationChanged?.Invoke(this, EventArgs.Empty);
    }

    protected abstract IEnumerable<WizardStepViewModel> BuildSteps();

    protected abstract TResult BuildResult();
}
