using FluentAssertions;
using Avalonia.Media;
using Avalonia.Styling;
using Arcana.App.Themes;

namespace Arcana.App.Tests.Themes;

public class ColorThemeCatalogTests
{
    private static readonly string[] ExpectedIds =
    {
        "arcanamystic", "akc12", "aquaverse", "blk-36", "brewerviridis",
        "neon-space", "shido-cyberneon", "slimy-05", "soapy-10",
        "windows2000", "windowsxp", "beos",
    };

    private static readonly string[] RetroIds = { "windows2000", "windowsxp", "beos" };

    [Fact]
    public void All_ShouldContainEveryBuiltInTheme()
    {
        ColorThemeCatalog.All.Select(t => t.Id)
            .Should().BeEquivalentTo(ExpectedIds);
    }

    [Fact]
    public void EachTheme_ShouldHaveUniqueId()
    {
        ColorThemeCatalog.All.Select(t => t.Id).Distinct().Count()
            .Should().Be(ColorThemeCatalog.All.Count);
    }

    [Fact]
    public void Default_ShouldBeBrewerViridis()
    {
        ColorThemeCatalog.Default.Id.Should().Be("brewerviridis");
    }

    [Fact]
    public void ArcanaMystic_ShouldPreserveOriginalPalette()
    {
        var theme = ColorThemeCatalog.ArcanaMystic;
        theme.Background.Should().Be(Color.Parse("#16161E"));
        theme.TextPrimary.Should().Be(Color.Parse("#E4E4EE"));
        theme.Accent.Should().Be(Color.Parse("#8B5CF6"));
    }

    [Fact]
    public void PaletteThemes_ShouldBeDarkVariant()
    {
        foreach (var id in ExpectedIds.Except(RetroIds))
            ColorThemeCatalog.Find(id)!.Variant.Should().Be(ThemeVariant.Dark, id);
    }

    [Fact]
    public void RetroThemes_ShouldBeLightVariant()
    {
        foreach (var id in RetroIds)
            ColorThemeCatalog.Find(id)!.Variant.Should().Be(ThemeVariant.Light, id);
    }

    [Fact]
    public void EachTheme_ShouldHaveUsableContrastAndOpaqueTokens()
    {
        foreach (var theme in ColorThemeCatalog.All)
        {
            theme.Background.Should().NotBe(theme.TextPrimary, theme.Id);
            theme.Background.Should().NotBe(theme.Accent, theme.Id);
            foreach (var (_, color) in theme.TokenColors())
                color.A.Should().Be(255, $"{theme.Id} token");
        }
    }

    [Fact]
    public void BrewerViridis_ShouldDeriveExpectedTokens()
    {
        var theme = ColorThemeCatalog.Find("brewerviridis")!;
        theme.Background.Should().Be(Color.Parse("#000000"));
        theme.Surface.Should().Be(Color.Parse("#402859"));
        theme.TextPrimary.Should().Be(Color.Parse("#FFFFFF"));
        theme.TextSecondary.Should().Be(Color.Parse("#FFE5BF"));
        theme.Accent.Should().Be(Color.Parse("#D89544"));
        theme.Success.Should().Be(Color.Parse("#388771"));
        theme.Warning.Should().Be(Color.Parse("#FFEA63"));
    }
}
