using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Arcana.App.Localization;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels;

public partial class DeleteFilesConfirmViewModel : ObservableObject
{
    public DeleteFilesConfirmViewModel(IReadOnlyList<string> files)
    {
        Files = new ObservableCollection<string>(files);
        Title = T("dialog.deleteFiles.title");
        Message = T("dialog.deleteFiles.message");
        ConfirmText = string.Format(T("dialog.deleteFiles.confirm"), files.Count);
        CancelText = T("dialog.deleteFiles.cancel");
    }

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public ObservableCollection<string> Files { get; }

    public long TotalSize
    {
        get
        {
            long total = 0;
            foreach (var file in Files)
            {
                try
                {
                    if (System.IO.File.Exists(file))
                        total += new System.IO.FileInfo(file).Length;
                }
                catch { }
            }
            return total;
        }
    }

    public string TotalSizeFormatted => Arcana.App.Services.ByteFormat.Format(TotalSize);

    public bool Confirmed { get; private set; }

    public void Confirm() => Confirmed = true;
}