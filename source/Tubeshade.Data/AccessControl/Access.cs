using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.AccessControl;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class Access : SmartEnum<Access, int>
{
    public static readonly Access Read = new("read", 1);
    public static readonly Access Append = new("append", 2);
    public static readonly Access Modify = new("modify", 3);
    public static readonly Access Delete = new("delete", 4);
    public static readonly Access Owner = new("owner", 5);

    private Access(string name, int value)
        : base(name, value)
    {
    }
}
