using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class ArchiveOperationsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "arcana-operations", Guid.NewGuid().ToString("N"));

    public ArchiveOperationsTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task CorruptedEntry_DoesNotReplaceExistingFile()
    {
        using var vfs = new VirtualFileSystem();
        var node = vfs.AddFile("existing.txt", new MemoryStream("corrupted"u8.ToArray()));
        node.Crc32 = 123;
        var target = Path.Combine(_directory, "existing.txt");
        File.WriteAllText(target, "original");
        Func<Task> extract = () => ArchiveExtractor.ExtractAsync([node], _directory, overwrite: true);
        await extract.Should().ThrowAsync<InvalidDataException>();
        File.ReadAllText(target).Should().Be("original");
        Directory.GetFiles(_directory).Should().ContainSingle();
    }

    [Fact]
    public async Task KeepBothWithApplyToAll_PreservesAllFiles()
    {
        using var vfs = new VirtualFileSystem();
        foreach (var name in new[] { "a.txt", "b.txt" })
        {
            File.WriteAllText(Path.Combine(_directory, name), "original");
            vfs.AddFile(name, new MemoryStream("new"u8.ToArray()));
        }
        var prompts = 0;
        await ArchiveExtractor.ExtractAsync([vfs.Root], _directory, resolveConflict: _ =>
        {
            prompts++;
            return Task.FromResult(new ConflictDecision(ConflictAction.Rename, ApplyToAll: true));
        });
        prompts.Should().Be(1);
        File.ReadAllText(Path.Combine(_directory, "a.txt")).Should().Be("original");
        File.ReadAllText(Path.Combine(_directory, "a (1).txt")).Should().Be("new");
        File.ReadAllText(Path.Combine(_directory, "b (1).txt")).Should().Be("new");
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("folder/../../outside.txt")]
    [InlineData("C:/outside.txt")]
    [InlineData("//server/share/file")]
    [InlineData("file.txt:stream")]
    [InlineData("NUL.txt")]
    [InlineData("folder./file")]
    public void UnsafePaths_AreRejected(string path)
    {
        Action act = () => ArchiveExtractor.GetDestinationPath(_directory, path);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public async Task SelectedFile_AndEmptyDirectory_AreExtracted()
    {
        using var vfs = new VirtualFileSystem();
        var selected = vfs.AddFile("folder/selected.txt", new MemoryStream("selected"u8.ToArray()));
        vfs.AddFile("other.txt", new MemoryStream("other"u8.ToArray()));
        var empty = vfs.AddDirectory("empty");
        var result = await ArchiveExtractor.ExtractAsync([selected, empty], _directory);
        result.Extracted.Should().Be(1);
        File.ReadAllText(Path.Combine(_directory, "folder", "selected.txt")).Should().Be("selected");
        Directory.Exists(Path.Combine(_directory, "empty")).Should().BeTrue();
        File.Exists(Path.Combine(_directory, "other.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task ExistingFile_IsSkippedUnlessOverwriteWasRequested()
    {
        using var vfs = new VirtualFileSystem();
        vfs.AddFile("existing.txt", new MemoryStream("new"u8.ToArray()));
        var target = Path.Combine(_directory, "existing.txt");
        File.WriteAllText(target, "original");
        (await ArchiveExtractor.ExtractAsync([vfs.Root], _directory)).Skipped.Should().Be(1);
        File.ReadAllText(target).Should().Be("original");
        (await ArchiveExtractor.ExtractAsync([vfs.Root], _directory, overwrite: true)).Overwritten.Should().Be(1);
        File.ReadAllText(target).Should().Be("new");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOrCancelledWrite_PreservesDestinationAndRemovesTemporary(bool cancel)
    {
        var target = Path.Combine(_directory, "existing.zip");
        File.WriteAllText(target, "original");
        using var cts = new CancellationTokenSource();
        Func<Task> act = () => AtomicFile.WriteAsync(target, async (stream, ct) =>
        {
            await stream.WriteAsync("incomplete"u8.ToArray(), ct);
            if (cancel) cts.Cancel();
            else throw new IOException("Simulated write failure");
        }, cts.Token);
        await act.Should().ThrowAsync<Exception>();
        File.ReadAllText(target).Should().Be("original");
        Directory.GetFiles(_directory).Should().ContainSingle();
    }

    [Fact]
    public async Task TraversalIsValidatedBeforeAnyFileIsWritten()
    {
        using var vfs = new VirtualFileSystem();
        vfs.AddFile("valid.txt", new MemoryStream("hello"u8.ToArray()));
        vfs.AddFile("../outside.txt", new MemoryStream("invalid"u8.ToArray()));
        Func<Task> act = () => ArchiveExtractor.ExtractAsync([vfs.Root], _directory);
        await act.Should().ThrowAsync<InvalidDataException>();
        Directory.GetFiles(_directory).Should().BeEmpty();
    }

    [Fact]
    public async Task SaveUsesDestinationFormat_AndCanReplaceAnOpenedArchive()
    {
        using var vfs = new VirtualFileSystem();
        vfs.AddFile("hello.txt", new MemoryStream("hello"u8.ToArray()));
        using var archive = new Archive { Vfs = vfs, FormatEngine = new ZipEngine() };
        var path = Path.Combine(_directory, "archive.zip");
        await ArchiveWriter.SaveAsync(archive, path, new CompressionOptions());
        using var opened = await ArchiveFactory.OpenAsync(path);
        await ArchiveWriter.SaveAsync(opened, path, new CompressionOptions());
        var converted = Path.Combine(_directory, "converted.7z");
        await ArchiveWriter.SaveAsync(opened, converted, new CompressionOptions { Format = CompressionFormat.SevenZip });
        using var reopened = await ArchiveFactory.OpenAsync(converted);
        reopened.Format.Should().Be(CompressionFormat.SevenZip);
        using var content = reopened.Vfs.FindNode("hello.txt")!.OpenRead();
        new StreamReader(content).ReadToEnd().Should().Be("hello");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
