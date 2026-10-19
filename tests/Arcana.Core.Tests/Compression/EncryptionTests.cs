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
    public void SevenZipEngine_LegacyEnvelope_ShouldRemainReadable()
    {
        var engine = new SevenZipEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        WriteLegacySevenZip(engine, archive, output);

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

        WriteLegacySevenZip(engine, archive, output);

        output.Position = 0;
        engine.Password = WrongPassword;
        var act = () => engine.Open("secret.7z", output, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ZstdEngine_LegacyEnvelope_ShouldRemainReadable()
    {
        var engine = new ZstdEngine();
        using var output = new MemoryStream();
        var archive = CreateArchive(engine, "secret.txt", Lorem);

        WriteLegacyZstd(engine, archive, output);

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

        WriteLegacyZstd(engine, archive, output);

        output.Position = 0;
        engine.Password = WrongPassword;
        var act = () => engine.Open("secret.zst", output, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void SevenZipEncryptedWrite_IsExplicitlyUnavailable()
    {
        var engine = new SevenZipEngine();
        using var archive = CreateArchive(engine, "secret.txt", Lorem);
        using var output = new MemoryStream();
        Action act = () => engine.Save(archive, output, new CompressionOptions
        {
            Format = CompressionFormat.SevenZip,
            Encryption = new EncryptionOptions { Password = Password },
        });
        act.Should().Throw<NotSupportedException>();
        output.Length.Should().Be(0);
    }

    [Fact]
    public void EncryptedZip_IsReadableByIndependentStandardReader()
    {
        var engine = new ZipEngine();
        using var archive = CreateArchive(engine, "secret.txt", Lorem);
        using var output = new MemoryStream();
        engine.Save(archive, output, new CompressionOptions { Encryption = new EncryptionOptions { Password = Password } });
        output.ToArray().Take(2).Should().Equal(0x50, 0x4B);
        output.Position = 0;
        using var standard = SharpCompress.Archives.Zip.ZipArchive.OpenArchive(output,
            new SharpCompress.Readers.ReaderOptions { Password = Password });
        using var content = standard.Entries.Single().OpenEntryStream();
        using var decoded = new MemoryStream();
        content.CopyTo(decoded);
        decoded.ToArray().Should().Equal(Lorem);
    }

    private static void WriteLegacySevenZip(SevenZipEngine engine, Archive archive, Stream output)
    {
        using var plain = new MemoryStream();
        engine.Save(archive, plain, new CompressionOptions { Format = CompressionFormat.SevenZip });
        plain.Position = 0;
        using var encrypted = new EncryptionProvider(new EncryptionOptions { Password = Password }).CreateEncryptingStream(output);
        plain.CopyTo(encrypted);
    }

    private static void WriteLegacyZstd(ZstdEngine engine, Archive archive, Stream output)
    {
        using var plain = new MemoryStream();
        engine.Save(archive, plain, new CompressionOptions { Format = CompressionFormat.Zstandard });
        plain.Position = 0;
        using var encrypted = new EncryptionProvider(new EncryptionOptions { Password = Password }).CreateEncryptingStream(output);
        plain.CopyTo(encrypted);
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
