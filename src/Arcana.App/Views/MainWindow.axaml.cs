using System;
using System.IO;
using Avalonia.Controls;
using Arcana.App.ViewModels;

namespace Arcana.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IconThemes.Changed -= OnThemeChanged;
            vm.IconThemes.Changed += OnThemeChanged;
            ApplyThemeIcon();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyThemeIcon();
    }

    private void ApplyThemeIcon()
    {
        if (DataContext is not MainViewModel vm)
            return;

        var path = vm.IconThemes.CurrentWindowIconPath;
        if (path == null || !File.Exists(path))
            return;

        try
        {
            using var stream = File.OpenRead(path);
            Icon = new WindowIcon(stream);
        }
        catch (Exception)
        {
            // keep the default icon when a theme icon cannot be decoded
        }
    }
}
