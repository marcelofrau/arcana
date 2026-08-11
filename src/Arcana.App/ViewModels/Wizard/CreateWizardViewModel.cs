using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arcana.App.Icons;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels.Wizard;

public sealed class CreateWizardViewModel : WizardViewModel<CreateWizardResult>
{
    public CreateWizardViewModel(string? defaultPath = null)
    {
        if (!string.IsNullOrWhiteSpace(defaultPath))
            _destinationStep.ArchivePath = defaultPath;
    }

    private readonly CreateDestinationStep _destinationStep = new();
    private readonly CreateFilesStep _filesStep = new();
    private readonly CreatePasswordStep _passwordStep = new();

    public override string Title => T("wizard.create.title");
    public IconKey HeaderIcon => IconKey.FileArchive;

    protected override IEnumerable<WizardStepViewModel> BuildSteps()
        => new WizardStepViewModel[] { _destinationStep, _filesStep, _passwordStep };

    protected override CreateWizardResult BuildResult() => new(
        _destinationStep.ArchivePath.Trim(),
        _destinationStep.SelectedFormat.Value,
        _destinationStep.Level,
        _filesStep.Files.Select(f => f.Path).ToList(),
        _passwordStep.Password);
}

public sealed partial class CreateDestinationStep : WizardStepViewModel
{
    [ObservableProperty]
    private string _archivePath = "";

    public IReadOnlyList<FormatOption> Formats => WizardFormats.All;

    [ObservableProperty]
    private FormatOption _selectedFormat = WizardFormats.Default;

    [ObservableProperty]
    private int _level = 5;

    public override string Title => T("wizard.create.destTitle");
    public override string Description => T("wizard.create.destDesc");

    public string LevelLabel => string.Format(T("wizard.create.levelValue"), Level);

    public override bool CanProceed => !string.IsNullOrWhiteSpace(ArchivePath);

    partial void OnArchivePathChanged(string value) => NotifyStateChanged();
    partial void OnLevelChanged(int value) => OnPropertyChanged(nameof(LevelLabel));
}

public sealed partial class CreateFilesStep : WizardStepViewModel
{
    public ObservableCollection<FileItem> Files { get; } = [];

    public override string Title => T("wizard.create.filesTitle");
    public override string Description => T("wizard.create.filesDesc");

    public void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(File.Exists))
            Files.Add(new FileItem(Path.GetFileName(path), path));
        NotifyStateChanged();
    }

    [RelayCommand]
    public void Remove(FileItem item)
    {
        Files.Remove(item);
        NotifyStateChanged();
    }
}

public sealed record FileItem(string Name, string Path);

public sealed partial class CreatePasswordStep : WizardStepViewModel
{
    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _confirm = "";

    public override string Title => T("wizard.create.passwordTitle");
    public override string Description => T("wizard.create.passwordDesc");

    public override bool CanProceed => Password == Confirm;

    partial void OnPasswordChanged(string value) => NotifyStateChanged();
    partial void OnConfirmChanged(string value) => NotifyStateChanged();
}
