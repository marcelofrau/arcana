using System.Linq;
using Arcana.App.ViewModels;
using Arcana.Core.Compression;
using FluentAssertions;
using Xunit;

namespace Arcana.App.Tests.ViewModels;

public class ExtractCompleteViewModelTests
{
    private static ExtractedFile File(int i, ExtractStatus status = ExtractStatus.Extracted)
        => new($"dir/file{i}.bin", status);

    private static ExtractCompleteViewModel Vm(int count)
        => new("t", "s", "c:\\out", Enumerable.Range(0, count).Select(i => File(i)).ToList());

    [Fact]
    public void LargeResult_StartsWithOnePage()
    {
        var vm = Vm(500);

        vm.Files.Count.Should().Be(500);
        vm.ShownFiles.Should().HaveCount(ExtractCompleteViewModel.PageSize);
        vm.RemainingFiles.Should().Be(500 - ExtractCompleteViewModel.PageSize);
        vm.HasMore.Should().BeTrue();
    }

    [Fact]
    public void LoadMore_PagesUntilEveryFileIsListed()
    {
        var vm = Vm(500);

        while (vm.HasMore)
            vm.LoadMoreCommand.Execute(null);

        vm.ShownFiles.Should().HaveCount(500);
        vm.RemainingFiles.Should().Be(0);
        vm.ShownFiles.Select(f => f.RelativePath).Should().OnlyHaveUniqueItems();
        vm.LoadMoreText.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SmallResult_ListsEverythingWithoutLoadMore()
    {
        var vm = Vm(2);

        vm.ShownFiles.Should().HaveCount(2);
        vm.HasMore.Should().BeFalse();
    }
}
