using System.Collections.Generic;
using System.Linq;
using Arcana.App.Icons;
using Avalonia.Media;
using FluentAssertions;
using Xunit;

namespace Arcana.App.Tests;

public class CompositeIconProviderTests
{
    private sealed class RecordingProvider : IIconProvider
    {
        public string Name { get; set; } = "recording";
        public double ToolbarSize { get; set; } = 48;
        public List<IconKey> Called { get; } = new();

        public IImage? GetIcon(IconKey key)
        {
            Called.Add(key);
            return null;
        }
    }

    private static (CompositeIconProvider Composite, RecordingProvider Fs, RecordingProvider Action) Create()
    {
        var fs = new RecordingProvider { Name = "fs" };
        var action = new RecordingProvider { Name = "action" };
        return (new CompositeIconProvider(fs, action), fs, action);
    }

    [Theory]
    [InlineData(IconKey.Folder)]
    [InlineData(IconKey.FileGeneric)]
    [InlineData(IconKey.FileArchive)]
    [InlineData(IconKey.FileImage)]
    [InlineData(IconKey.FileCode)]
    [InlineData(IconKey.FileMedia)]
    [InlineData(IconKey.FileDoc)]
    [InlineData(IconKey.Rar)]
    public void FilesystemKeys_ShouldRouteToFilesystemProvider(IconKey key)
    {
        var (composite, fs, action) = Create();

        composite.GetIcon(key);

        fs.Called.Should().Contain(key);
        action.Called.Should().BeEmpty();
    }

    [Theory]
    [InlineData(IconKey.Open)]
    [InlineData(IconKey.Add)]
    [InlineData(IconKey.Extract)]
    [InlineData(IconKey.Test)]
    [InlineData(IconKey.View)]
    [InlineData(IconKey.Delete)]
    [InlineData(IconKey.Find)]
    [InlineData(IconKey.Info)]
    [InlineData(IconKey.SortUp)]
    [InlineData(IconKey.SortDown)]
    [InlineData(IconKey.Save)]
    [InlineData(IconKey.Settings)]
    [InlineData(IconKey.Help)]
    public void ActionKeys_ShouldRouteToActionProvider(IconKey key)
    {
        var (composite, fs, action) = Create();

        composite.GetIcon(key);

        action.Called.Should().Contain(key);
        fs.Called.Should().BeEmpty();
    }

    [Fact]
    public void ToolbarSize_ShouldComeFromActionProvider()
    {
        var (composite, _, action) = Create();
        action.ToolbarSize = 24;

        composite.ToolbarSize.Should().Be(24);
    }

    [Fact]
    public void Name_ShouldComeFromActionProvider()
    {
        var (composite, _, action) = Create();
        action.Name = "Tango";

        composite.Name.Should().Be("Tango");
    }
}
