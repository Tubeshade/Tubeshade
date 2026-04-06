using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tubeshade.Server.Tests.Pages.Shared;

public sealed class LinesTestCaseSource : IEnumerable<TestCaseData<string, int, string[], int, string[]>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, int, string[], int, string[]>> GetEnumerator()
    {
        yield return new(
            """
            foo

            bar
            """,
            3,
            ["foo", "", "bar"],
            2,
            ["foo", "bar"])
        {
            TestName = "Empty line is ignored",
        };

        yield return new(
            """
            foo
                
            bar
            """,
            3,
            ["foo", "    ", "bar"],
            2,
            ["foo", "bar"])
        {
            TestName = "Whitespace line is ignored",
        };

        yield return new(
            """
              foo  

              bar  
            """,
            3,
            ["  foo  ", "", "  bar  "],
            2,
            ["foo", "bar"])
        {
            TestName = "Lines are trimmed",
        };
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
