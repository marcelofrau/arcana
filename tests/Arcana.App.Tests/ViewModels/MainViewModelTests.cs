using System.IO;
using FluentAssertions;
using Arcana.App.ViewModels;

namespace Arcana.App.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly MainViewModel _sut = new(
        new Services.ArchiveService(),
        new Services.PreviewService(),
        new Services.DialogService(SettingsServiceTemp()),
        new Arcana.App.Icons.IconThemeService(new Arcana.App.Icons.DefaultIconProvider(), SettingsServiceTemp()),
        new Arcana.App.Icons.DefaultIconProvider(),
        SettingsServiceTemp(),
        new Services.FavoritesService(),
        new Arcana.App.Themes.ColorThemeService(SettingsServiceTemp()));

    private static Services.SettingsService SettingsServiceTemp()
        => new(Path.Combine(Path.GetTempPath(), "arcana-tests", Guid.NewGuid().ToString("N") + ".json"));

    [Fact]
    public void StatusText_ShouldDefaultToReady()
    {
        _sut.StatusText.Should().Be("Ready");
    }

    [Fact]
    public void ArchiveTree_ShouldBeEmptyOnCreation()
    {
        _sut.Archive.TreeNodes.Should().BeEmpty();
    }

    [Fact]
    public void OpenArchiveCommand_ShouldNotThrow()
    {
        var act = () => _sut.OpenArchiveCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void ColorThemeMenuItems_ShouldContainEveryCatalogTheme()
    {
        _sut.ColorThemeMenuItems.Select(i => i.Name)
            .Should().BeEquivalentTo(Arcana.App.Themes.ColorThemeCatalog.All.Select(t => t.Id));
    }

    [Fact]
    public void ColorThemeMenuItems_ShouldMarkDefaultAsCurrent()
    {
        var expected = Arcana.App.Themes.ColorThemeCatalog.Default.Id;
        _sut.ColorThemeMenuItems.Single(i => i.IsCurrent).Name.Should().Be(expected);
    }

    [Fact]
    public void ThemeMenuItems_ShouldContainEveryBuiltInTheme()
    {
        _sut.ThemeMenuItems.Select(i => i.Name)
            .Should().BeEquivalentTo(_sut.IconThemes.BuiltInThemes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ThemeMenuItems_ShouldMarkNumixAsCurrentByDefault()
    {
        _sut.ThemeMenuItems.Single(i => i.IsCurrent).Name.Should().Be("Numix");
    }
}
