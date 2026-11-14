namespace Tubeshade.Data.AccessControl;

public interface IAccessParameters
{
    long PrincipalId { get; }

    Permissions Permissions { get; }
}
