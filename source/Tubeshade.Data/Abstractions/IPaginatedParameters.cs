namespace Tubeshade.Data.Abstractions;

public interface IPaginatedParameters
{
    int Limit { get; }

    int Offset { get; }
}
