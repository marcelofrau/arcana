using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Vfs;

public class VirtualFileSystemTests
{
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
