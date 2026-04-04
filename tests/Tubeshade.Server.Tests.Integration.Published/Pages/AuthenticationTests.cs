using System;
using System.Threading.Tasks;
using FluentAssertions;
using Htmx;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.Pages;

public sealed class AuthenticationTests(IServerFixture fixture) : PlaywrightTests(fixture)
{
    [Test]
    public async Task HtmxRequests_ShouldNotRedirectToLogin()
    {
        await Page.GotoAsync("/Tasks");

        var successfulTasks = false;
        for (var i = 0; i < 10; i++)
        {
            var request = await Page.WaitForRequestFinishedAsync(new() { Predicate = TasksRequestPredicate });
            var response = await request.ResponseAsync();
            successfulTasks = response?.Status is StatusCodes.Status200OK;

            if (successfulTasks)
            {
                break;
            }

            await Task.Delay(1_000);
        }

        successfulTasks.Should().BeTrue();

        var otherPage = await Context.NewPageAsync();
        await otherPage.GotoAsync("/");
        await otherPage.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();

        if (!await otherPage.GetByText("Use a local account to log in.").IsVisibleAsync())
        {
            Console.WriteLine(await otherPage.Locator("body").AriaSnapshotAsync());
            Assert.Fail("Failed to log out");
        }

        var failedTasks = false;
        for (var i = 0; i < 10; i++)
        {
            var request = await Page.WaitForRequestFinishedAsync(new() { Predicate = TasksRequestPredicate });
            var response = await request.ResponseAsync();
            failedTasks = response?.Status is StatusCodes.Status401Unauthorized;

            if (failedTasks)
            {
                break;
            }

            await Task.Delay(1_000);
        }

        failedTasks.Should().BeTrue();
    }

    private static bool TasksRequestPredicate(IRequest request)
    {
        return request.Url.Contains("/Tasks") &&
               request.Headers.ContainsKey(HtmxRequestHeaders.Keys.Request.ToLowerInvariant());
    }
}
