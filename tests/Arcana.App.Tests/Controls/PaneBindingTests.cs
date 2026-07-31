using FluentAssertions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Arcana.App.Controls;
using Arcana.App.Services;
using Arcana.App.ViewModels;
using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;

namespace Arcana.App.Tests.Controls;

public class PaneBindingTests
{
    private static MainViewModel CreateMainVm() => new(
        new Services.ArchiveService(),
        new Services.PreviewService(),
        new Services.DialogService(new Services.SettingsService()),
        new Icons.IconThemeService(new Icons.DefaultIconProvider(), new Services.SettingsService()),
        new Icons.DefaultIconProvider(),
        new Services.SettingsService(),
        new Services.FavoritesService(),
        new Arcana.App.Themes.ColorThemeService(new Services.SettingsService()));

    private static async Task<Archive> CreateZipArchiveAsync(params (string Path, string Content)[] files)
    {
        var engine = new ZipEngine();
        var vfs = new VirtualFileSystem();
        foreach (var (path, content) in files)
            vfs.AddFile(path, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));
        var saveArchive = new Archive
        {
            Format = CompressionFormat.Zip,
            FormatEngine = engine,
            Vfs = vfs,
            Entries = Array.Empty<ArchiveEntry>(),
        };

        var ms = new MemoryStream();
        await engine.SaveAsync(saveArchive, ms, new CompressionOptions());
        ms.Position = 0;

        return await engine.OpenAsync("test.zip", ms, AccessMode.Read);
    }

    private static T FindByName<T>(Control root, string name) where T : Control
    {
        var found = root.FindControl<T>(name);
        found.Should().NotBeNull($"expected named element '{name}'");
        return found!;
    }

    [Fact]
    public async Task ArchiveViewModel_LoadArchive_PopulatesTreeAndEntries()
    {
        var archive = await CreateZipArchiveAsync(("hello.txt", "hello world"));
        var vm = new ArchiveViewModel();
        vm.LoadArchive(archive, "test.zip");

        vm.TreeNodes.Should().ContainSingle();
        vm.Root!.Name.Should().Be("test.zip");
        var entry = vm.Entries.Should().ContainSingle(e => e.Name == "hello.txt").Subject;
        entry.SizeText.Should().Be("11 B");
        entry.PackedText.Should().NotBeEmpty();
        entry.RatioText.Should().NotBeEmpty();
        entry.ModifiedText.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FileTable_ShowsEntries_AfterLoadArchive()
    {
        var archive = await CreateZipArchiveAsync(("hello.txt", "hello world"));
        var main = CreateMainVm();
        main.Archive.LoadArchive(archive, "test.zip");

        var table = new FileTable { DataContext = main };
        var window = new Window { Width = 800, Height = 600, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var grid = FindByName<DataGrid>(table, "Grid");
            grid.ItemsSource.Should().BeSameAs(main.Archive.Entries);
            grid.ApplyTemplate();
            grid.Template.Should().NotBeNull();
            Dispatcher.UIThread.RunJobs();

            var rows = grid.GetVisualDescendants().OfType<DataGridRow>().ToList();
            rows.Should().HaveCount(1);
            rows[0].DataContext.Should().BeSameAs(main.Archive.Entries[0]);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task FolderTree_ShowsRootNode_AfterLoadArchive()
    {
        var archive = await CreateZipArchiveAsync(("hello.txt", "hello world"));
        var main = CreateMainVm();
        main.Archive.LoadArchive(archive, "test.zip");

        var tree = new FolderTree { DataContext = main };
        var window = new Window { Content = tree };
        window.Show();

        try
        {
            var tv = FindByName<TreeView>(tree, "Tree");
            tv.Items.Count.Should().Be(1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task ArchiveViewModel_LoadArchive_ChildFoldersFiltersFiles()
    {
        var archive = await CreateZipArchiveAsync(
            ("hello.txt", "hello world"),
            ("docs/readme.txt", "read me"));
        var vm = new ArchiveViewModel();
        vm.LoadArchive(archive, "test.zip");

        vm.Root!.ChildFolders.Select(f => f.Name)
            .Should().BeEquivalentTo(new[] { "docs" });
        vm.Entries.Select(e => e.Name)
            .Should().BeEquivalentTo("docs", "hello.txt");
    }

    [Fact]
    public async Task FolderTree_ShowsOnlyFolders_AfterLoadArchive()
    {
        var archive = await CreateZipArchiveAsync(
            ("hello.txt", "hello world"),
            ("docs/readme.txt", "read me"));
        var main = CreateMainVm();
        main.Archive.LoadArchive(archive, "test.zip");

        var tree = new FolderTree { DataContext = main };
        var window = new Window { Width = 800, Height = 600, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var tv = FindByName<TreeView>(tree, "Tree");
            var rootItem = tv.ContainerFromIndex(0) as TreeViewItem;
            rootItem.Should().NotBeNull();
            rootItem!.IsExpanded.Should().BeTrue();
            rootItem.ItemCount.Should().Be(1);
            var docsItem = rootItem.ContainerFromIndex(0) as TreeViewItem;
            docsItem.Should().NotBeNull();
            (docsItem!.DataContext as ArchiveNode)!.Name.Should().Be("docs");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void PreviewViewModel_Show_LoadsTextForTxtEntry()
    {
        var node = new ArchiveNode
        {
            Name = "hello.txt",
            FullPath = "/hello.txt",
            Type = NodeType.File,
            OriginalSize = 11,
            ContentFactory = () => new MemoryStream("hello world"u8.ToArray()),
        };
        var item = new FileEntryItem(node);
        var vm = new PreviewViewModel(new PreviewService());
        vm.Show(item);

        vm.IsVisible.Should().BeTrue();
        vm.Kind.Should().Be(PreviewKind.Text);
        vm.FileName.Should().Be("hello.txt");
        vm.TextContent.Should().Be("hello world");
    }

    [Fact]
    public void PreviewViewModel_Show_HexShowsPlaceholderUntilBinaryRequested()
    {
        var node = new ArchiveNode
        {
            Name = "data.bin",
            FullPath = "/data.bin",
            Type = NodeType.File,
            Parent = new ArchiveNode { Name = "root", FullPath = "/", Type = NodeType.Directory },
            OriginalSize = 4,
            ContentFactory = () => new MemoryStream(new byte[] { 0x00, 0x01, 0xFE, 0xFF }),
        };
        var item = new FileEntryItem(node);
        var vm = new PreviewViewModel(new PreviewService());
        vm.Show(item);

        vm.IsVisible.Should().BeTrue();
        vm.Kind.Should().Be(PreviewKind.Hex);
        vm.IsBinaryPlaceholder.Should().BeTrue();
        vm.TextContent.Should().BeEmpty();
        vm.FileName.Should().Be("data.bin");
        vm.PlaceholderIcon.Should().Be(Icons.IconKey.FileGeneric);

        vm.LoadBinaryCommand.Execute(null);

        vm.IsBinaryPlaceholder.Should().BeFalse();
        vm.TextContent.Should().NotBeEmpty();
        vm.TextContent.Should().Contain("00 01 FE FF");
    }

    [Fact]
    public async Task SelectionChange_RaisesPreview_EndToEnd()
    {
        var archive = await CreateZipArchiveAsync(("hello.txt", "hello world"));
        var main = CreateMainVm();
        main.Archive.LoadArchive(archive, "test.zip");
        main.Archive.SelectedItem = main.Archive.Entries[0];

        main.Preview.IsVisible.Should().BeTrue();
        main.Preview.Kind.Should().Be(PreviewKind.Text);
        main.Preview.TextContent.Should().Be("hello world");
    }
}
