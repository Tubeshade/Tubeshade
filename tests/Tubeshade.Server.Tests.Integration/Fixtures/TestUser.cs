using System;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

/// <param name="OwnerId">The id of the user's personal owner.</param>
internal sealed record TestUser(Guid Id, Guid OwnerId);
