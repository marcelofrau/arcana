using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Vfs;

public class VirtualFileSystemTests
{
    [Fact]
    public void RenamingFolder_UpdatesDescendantsAndMarksChanges()
    {
        using var vfs = new VirtualFileSystem();
        var child = vfs.AddFile("old/sub/file.txt", new MemoryStream("hello"u8.ToArray()));
        vfs.MarkAllClean();
        vfs.RenameNode(vfs.FindNode("old")!, "new");
        child.FullPath.Should().Be("/new/sub/file.txt");
        vfs.FindNode("new/sub/file.txt").Should().BeSameAs(child);
        vfs.FindNode("old/sub/file.txt").Should().BeNull();
        vfs.HasDirtyNodes.Should().BeTrue();
    }

    [Fact]
    public void CaseSensitiveNames_AreNotMerged()
    {
        using var vfs = new VirtualFileSystem();
        var upper = vfs.AddFile("A/file", new MemoryStream());
        var lower = vfs.AddFile("a/file", new MemoryStream());
        vfs.FindNode("A/file").Should().BeSameAs(upper);
        vfs.FindNode("a/file").Should().BeSameAs(lower);
    }

    [Fact]
    public void RenameCannotIntroduceTraversalOrReplaceAnotherEntry()
    {
        using var vfs = new VirtualFileSystem();
        var file = vfs.AddFile("a", new MemoryStream());
        vfs.AddFile("b", new MemoryStream());
        Action traversal = () => vfs.RenameNode(file, "../outside");
        traversal.Should().Throw<ArgumentException>();
        Action collision = () => vfs.RenameNode(file, "b");
        collision.Should().Throw<IOException>();
        file.FullPath.Should().Be("/a");
    }

    [Fact]
    public void AddFile_SharedStream_SurvivesCallerDispose()
    {
        var vfs = new VirtualFileSystem();
        var shared = new MemoryStream("hello"u8.ToArray());
        var node = vfs.AddFile("test.txt", shared);

        using (var first = node.OpenRead())
        {
            new StreamReader(first).ReadToEnd().Should().Be("hello");
        }

        using var second = node.OpenRead();
        new StreamReader(second).ReadToEnd().Should().Be("hello");
    }

    [Fact]
    public void AddLazyFile_FreshStreamPerCall()
    {
        var vfs = new VirtualFileSystem();
        var node = vfs.AddLazyFile("test.txt", () => new MemoryStream("data"u8.ToArray()));

        using (var first = node.OpenRead())
        {
            new StreamReader(first).ReadToEnd().Should().Be("data");
        }

        using var second = node.OpenRead();
        new StreamReader(second).ReadToEnd().Should().Be("data");
    }
}
