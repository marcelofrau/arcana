using FluentAssertions;
using Arcana.App.ViewModels;

namespace Arcana.App.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly MainViewModel _sut = new(
        new Services.ArchiveService(),
        new Services.PreviewService(),
        new Services.DialogService(),
        new Icons.IconThemeService(new Icons.DefaultIconProvider()),
        new Icons.DefaultIconProvider());

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
}
