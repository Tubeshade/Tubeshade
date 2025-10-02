using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.AccessControl;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class Access : SmartEnum<Access, int>
{
    public static readonly Access Read = new(Names.Read, 1);
    public static readonly Access Append = new(Names.Append, 2);
    public static readonly Access Modify = new(Names.Modify, 3);
    public static readonly Access Delete = new(Names.Delete, 4);
    public static readonly Access Owner = new(Names.Owner, 5);

    private Access(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Read = "read";
        public const string Append = "append";
        public const string Modify = "modify";
        public const string Delete = "delete";
        public const string Owner = "owner";
    }
}
