using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Tests.Pages.Shared;

[TestOf(typeof(PaginatedData<>))]
public sealed class PaginatedDataTests
{
    [Test]
    public void Empty()
    {
        var paginatedData = new PaginatedData<int>
        {
            LibraryId = null,
            Data = [],
            Page = 0,
            PageSize = 20,
            TotalCount = 0,
        };

        using var scope = new AssertionScope();

        paginatedData.PageCount.Should().Be(0);
        paginatedData.StartIndex.Should().Be(1);
        paginatedData.EndIndex.Should().Be(0);
        paginatedData.IsFirst.Should().Be(true);
        paginatedData.IsLast.Should().Be(true);
        paginatedData.DisplayedPages.Should().BeEmpty();
    }

    [Test]
    public void SinglePage()
    {
        const int totalCount = 17;
        const int expectedPageCount = 1;

        var data = Enumerable.Range(1, totalCount).ToList();
        var paginatedData = new PaginatedData<int>
        {
            LibraryId = null,
            Data = data,
            Page = 0,
            PageSize = 20,
            TotalCount = totalCount,
        };

        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(1);
            paginatedData.EndIndex.Should().Be(totalCount);
            paginatedData.IsFirst.Should().Be(true);
            paginatedData.IsLast.Should().Be(true);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1]);
        }
    }

    [Test]
    public void FewPages()
    {
        const int totalCount = 53;
        const int expectedPageCount = 3;

        var data = Enumerable.Range(1, totalCount).ToList();
        var paginatedData = new PaginatedData<int>
        {
            LibraryId = null,
            Data = data,
            Page = 0,
            PageSize = 20,
            TotalCount = totalCount,
        };

        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(1);
            paginatedData.EndIndex.Should().Be(20);
            paginatedData.IsFirst.Should().Be(true);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1, 2, 3]);
        }

        paginatedData.Page = 1;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(21);
            paginatedData.EndIndex.Should().Be(40);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1, 2, 3]);
        }

        paginatedData.Page = 2;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(41);
            paginatedData.EndIndex.Should().Be(53);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(true);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1, 2, 3]);
        }
    }

    [Test]
    public void ManyPages()
    {
        const int totalCount = 1024;
        const int expectedPageCount = 52;

        var data = Enumerable.Range(1, totalCount).ToList();
        var paginatedData = new PaginatedData<int>
        {
            LibraryId = null,
            Data = data,
            Page = 0,
            PageSize = 20,
            TotalCount = totalCount,
        };

        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(1);
            paginatedData.EndIndex.Should().Be(20);
            paginatedData.IsFirst.Should().Be(true);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
        }

        paginatedData.Page = 1;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(21);
            paginatedData.EndIndex.Should().Be(40);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
        }

        paginatedData.Page = 2;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(41);
            paginatedData.EndIndex.Should().Be(60);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
        }

        paginatedData.Page = 3;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(61);
            paginatedData.EndIndex.Should().Be(80);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([2, 3, 4, 5, 6]);
        }

        paginatedData.Page = expectedPageCount - 4;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(961);
            paginatedData.EndIndex.Should().Be(980);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([47, 48, 49, 50, 51]);
        }

        paginatedData.Page = expectedPageCount - 3;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(981);
            paginatedData.EndIndex.Should().Be(1000);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([48, 49, 50, 51, 52]);
        }

        paginatedData.Page = expectedPageCount - 2;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(1001);
            paginatedData.EndIndex.Should().Be(1020);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(false);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([48, 49, 50, 51, 52]);
        }

        paginatedData.Page = expectedPageCount - 1;
        using (new AssertionScope())
        {
            paginatedData.PageCount.Should().Be(expectedPageCount);
            paginatedData.StartIndex.Should().Be(1021);
            paginatedData.EndIndex.Should().Be(totalCount);
            paginatedData.IsFirst.Should().Be(false);
            paginatedData.IsLast.Should().Be(true);
            paginatedData.DisplayedPages.Should().BeEquivalentTo([48, 49, 50, 51, 52]);
        }
    }
}
