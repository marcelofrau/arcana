using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Cryptography;
using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class EncryptionTests
{
    private const string Password = "correct-horse-battery-staple";
    private const string WrongPassword = "wrong-password";

    private static readonly byte[] Lorem = "Hello World, this is encrypted content!"u8.ToArray();

    [Fact]
    public void ZipEngine_SaveAndOpen_WithPassword_ShouldRoundTrip()
    {
        var engine = new ZipEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        var encOpts = new EncryptionOptions { Password = Password, KdfMemoryMB = 1 };
        engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.Zip,
            Encryption = encOpts,
        });

        output.Position = 0;
        engine.Password = Password;
        var opened = engine.Open("secret.zip", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("secret.txt");

        var node = opened.Vfs.FindNode("/secret.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(Lorem);
    }

    [Fact]
    public void ZipEngine_Open_WithWrongPassword_ShouldThrow()
    {
        var engine = new ZipEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        var encOpts = new EncryptionOptions { Password = Password, KdfMemoryMB = 1 };
        engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.Zip,
            Encryption = encOpts,
        });

        output.Position = 0;
        engine.Password = WrongPassword;
        var act = () => engine.Open("secret.zip", output, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void SevenZipEngine_SaveAndOpen_WithPassword_ShouldRoundTrip()
    {
        var engine = new SevenZipEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        var encOpts = new EncryptionOptions { Password = Password, KdfMemoryMB = 1 };
        engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.SevenZip,
            Encryption = encOpts,
        });

        output.Position = 0;
        engine.Password = Password;
        var opened = engine.Open("secret.7z", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("secret.txt");

        var node = opened.Vfs.FindNode("/secret.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(Lorem);
    }

    [Fact]
    public void SevenZipEngine_Open_WithWrongPassword_ShouldThrow()
    {
        var engine = new SevenZipEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        var encOpts = new EncryptionOptions { Password = Password, KdfMemoryMB = 1 };
        engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.SevenZip,
            Encryption = encOpts,
        });

        output.Position = 0;
        engine.Password = WrongPassword;
        var act = () => engine.Open("secret.7z", output, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ZstdEngine_SaveAndOpen_WithPassword_ShouldRoundTrip()
    {
        var engine = new ZstdEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        var encOpts = new EncryptionOptions { Password = Password, KdfMemoryMB = 1 };
        engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.Zstandard,
            Encryption = encOpts,
        });

        output.Position = 0;
        engine.Password = Password;
        var opened = engine.Open("secret.zst", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);

        var node = opened.Vfs.FindNode("/secret");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(Lorem);
    }

    [Fact]
    public void ZstdEngine_Open_WithWrongPassword_ShouldThrow()
    {
        var engine = new ZstdEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        var encOpts = new EncryptionOptions { Password = Password, KdfMemoryMB = 1 };
        engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.Zstandard,
            Encryption = encOpts,
        });

        output.Position = 0;
        engine.Password = WrongPassword;
        var act = () => engine.Open("secret.zst", output, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    private static Archive CreateArchive(IArchiveFormat engine, string fileName, byte[] content)
    {
        var vfs = new VirtualFileSystem();
        vfs.AddFile(fileName, new MemoryStream(content));

        return new Archive
        {
            Format = CompressionFormat.Zip,
            FormatEngine = engine,
            Entries = new List<ArchiveEntry>
            {
                new()
                {
                    Path = fileName,
                    Name = fileName,
                    Size = content.Length,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow,
                },
            },
            Vfs = vfs,
        };
    }
}
