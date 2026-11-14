using System;

namespace Tubeshade.Data.AccessControl;

/// <remarks>Any combination is valid, including write-only access such as Create and Update without Read.</remarks>
[Flags]
public enum Permissions
{
    None = 0,

    Read = 1,

    Create = 2,

    Update = 4,

    /// <summary>Deleting entities, or moving them to another owner.</summary>
    Delete = 8,

    /// <summary>Managing the owner's grants, renaming and deleting the owner.</summary>
    Share = 16,

    All = Read | Create | Update | Delete | Share,
}
