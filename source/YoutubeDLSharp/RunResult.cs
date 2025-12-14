using System.Diagnostics.CodeAnalysis;

namespace YoutubeDLSharp;

/// <summary>
/// Encapsulates the output of a yt-dlp download operation.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class RunResult<T>
    where T : class
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool Success { get; }

    public string?[] ErrorOutput { get; }

    public T? Data { get; }

    private RunResult(bool success, string?[] error, T? result)
    {
        Success = success;
        ErrorOutput = error;
        Data = result;
    }

    public static RunResult<T> Successful(T result, string[] error) => new(true, error, result);

    public static RunResult<T> Failed(string[] error) => new(false, error, null);
}
