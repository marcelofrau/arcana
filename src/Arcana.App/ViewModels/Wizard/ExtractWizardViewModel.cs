using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Arcana.App.Icons;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels.Wizard;

public sealed class ExtractWizardViewModel : WizardViewModel<ExtractWizardResult>
{
    public ExtractWizardViewModel(string? defaultDestination = null, string? archiveFolderName = null)
    {
        if (!string.IsNullOrWhiteSpace(defaultDestination))
            _destinationStep.DestinationDir = defaultDestination;
        if (!string.IsNullOrWhiteSpace(archiveFolderName))
            _destinationStep.SubfolderName = archiveFolderName;
    }

    private readonly ExtractDestinationStep _destinationStep = new();
    private readonly ExtractOptionsStep _optionsStep = new();

    public override string Title => T("wizard.extract.title");
    public IconKey HeaderIcon => IconKey.Extract;

    protected override IEnumerable<WizardStepViewModel> BuildSteps()
        => new WizardStepViewModel[] { _destinationStep, _optionsStep };

    protected override ExtractWizardResult BuildResult() => new(
        _destinationStep.DestinationDir.Trim(),
        _destinationStep.SubfolderName.Trim(),
        _optionsStep.Overwrite,
        _optionsStep.PreserveStructure,
        _destinationStep.SmartSubfolder);
}

public sealed partial class ExtractDestinationStep : WizardStepViewModel
{
    [ObservableProperty]
    private string _destinationDir = "";

    [ObservableProperty]
    private string _subfolderName = "";

    [ObservableProperty]
    private bool _smartSubfolder = true;

    public override string Title => T("wizard.extract.destTitle");
    public override string Description => T("wizard.extract.destDesc");

    public override bool CanProceed => !string.IsNullOrWhiteSpace(DestinationDir);

    partial void OnDestinationDirChanged(string value) => NotifyStateChanged();
}

public sealed partial class ExtractOptionsStep : WizardStepViewModel
{
    [ObservableProperty]
    private bool _overwrite;

    [ObservableProperty]
    private bool _preserveStructure = true;

    public override string Title => T("wizard.extract.optionsTitle");
    public override string Description => T("wizard.extract.optionsDesc");
}
