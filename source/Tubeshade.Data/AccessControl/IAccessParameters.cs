using System;

namespace Tubeshade.Data.AccessControl;

public interface IAccessParameters
{
    Guid PrincipalId { get; }

    Permissions Permissions { get; }
}
