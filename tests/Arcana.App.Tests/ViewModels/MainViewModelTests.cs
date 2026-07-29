using FluentAssertions;
using Arcana.App.ViewModels;

namespace Arcana.App.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly MainViewModel _sut = new();

    [Fact]
    public void StatusText_ShouldDefaultToReady()
    {
        _sut.StatusText.Should().Be("Ready");
    }

    [Fact]
    public void ArchiveTree_ShouldBeEmptyOnCreation()
    {
        _sut.ArchiveTree.Should().BeEmpty();
    }

    [Fact]
    public void OpenArchiveCommand_ShouldNotThrow()
    {
        var act = () => _sut.OpenArchiveCommand.Execute(null);
        act.Should().NotThrow();
    }
}
