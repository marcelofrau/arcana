using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Arcana.App.Icons;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels.Wizard;

public sealed class ConvertWizardViewModel : WizardViewModel<ConvertWizardResult>
{
    public ConvertWizardViewModel(string? suggestedName = null)
    {
        if (!string.IsNullOrWhiteSpace(suggestedName))
            _destinationStep.OutputPath = suggestedName;
    }

    private readonly ConvertFormatStep _formatStep = new();
    private readonly ConvertDestinationStep _destinationStep = new();

    public override string Title => T("wizard.convert.title");
    public IconKey HeaderIcon => IconKey.Convert;

    protected override IEnumerable<WizardStepViewModel> BuildSteps()
        => new WizardStepViewModel[] { _formatStep, _destinationStep };

    protected override ConvertWizardResult BuildResult() => new(
        _formatStep.SelectedFormat.Value,
        _formatStep.Level,
        _destinationStep.OutputPath.Trim());
}

public sealed partial class ConvertFormatStep : WizardStepViewModel
{
    public IReadOnlyList<FormatOption> Formats => WizardFormats.All;

    [ObservableProperty]
    private FormatOption _selectedFormat = WizardFormats.Default;

    [ObservableProperty]
    private int _level = 5;

    public override string Title => T("wizard.convert.formatTitle");
    public override string Description => T("wizard.convert.formatDesc");

    public string LevelLabel => string.Format(T("wizard.create.levelValue"), Level);

    partial void OnLevelChanged(int value) => OnPropertyChanged(nameof(LevelLabel));
}

public sealed partial class ConvertDestinationStep : WizardStepViewModel
{
    [ObservableProperty]
    private string _outputPath = "";

    public override string Title => T("wizard.convert.destTitle");
    public override string Description => T("wizard.convert.destDesc");

    public override bool CanProceed => !string.IsNullOrWhiteSpace(OutputPath);

    partial void OnOutputPathChanged(string value) => NotifyStateChanged();
}
