namespace Tubeshade.Data.Tasks.Payloads;

public sealed class IndexPayload : PayloadBase
{
    public required string Url { get; init; }
}
