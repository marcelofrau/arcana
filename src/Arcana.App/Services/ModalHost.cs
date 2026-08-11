using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Serilog;

namespace Arcana.App.Services;

/// <summary>
/// Shows modal dialogs over a dimmed owner window so the dialog's focus is
/// emphasised. The dim is a semi-transparent overlay dropped on top of the
/// owner's root panel; it is removed when the dialog closes.
/// </summary>
public static class ModalHost
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ModalHost));
    private const byte DimAlpha = 150;

    public static async Task ShowAsync(Window owner, Window dialog)
    {
        var overlay = Attach(owner);
        WindowFade.Attach(dialog);
        try
        {
            await dialog.ShowDialog(owner);
        }
        finally
        {
            Detach(owner, overlay);
        }
    }

    public static async Task<TResult> ShowAsync<TResult>(Window owner, Window dialog)
    {
        var overlay = Attach(owner);
        WindowFade.Attach(dialog);
        try
        {
            return await dialog.ShowDialog<TResult>(owner);
        }
        finally
        {
            Detach(owner, overlay);
        }
    }

    /// <summary>Adds the dim overlay to <paramref name="owner"/>. Returns the overlay
    /// so callers with a fire-and-forget dialog can remove it via <see cref="Detach"/>.</summary>
    public static Border? Attach(Window owner)
    {
        if (owner?.Content is not Panel panel)
        {
            Log.Verbose("Owner {Owner} has no root panel; skipping dim overlay", owner?.Title);
            return null;
        }

        var overlay = new Border
        {
            IsHitTestVisible = true,
            Background = new SolidColorBrush(Color.FromArgb(DimAlpha, 0, 0, 0)),
            ZIndex = 100000,
        };

        if (panel is Grid grid)
        {
            if (grid.RowDefinitions.Count > 0)
            {
                Grid.SetRow(overlay, 0);
                Grid.SetRowSpan(overlay, grid.RowDefinitions.Count);
            }
            if (grid.ColumnDefinitions.Count > 0)
            {
                Grid.SetColumn(overlay, 0);
                Grid.SetColumnSpan(overlay, grid.ColumnDefinitions.Count);
            }
        }

        panel.Children.Add(overlay);
        Log.Debug("Dim overlay added to {Owner}", owner.Title);
        return overlay;
    }

    public static void Detach(Window owner, Border? overlay)
    {
        if (overlay?.Parent is Panel panel)
            panel.Children.Remove(overlay);
        Log.Debug("Dim overlay removed from {Owner}", owner?.Title);
    }
}
