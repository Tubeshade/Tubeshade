using System.Diagnostics.CodeAnalysis;

namespace YoutubeDLSharp;

/// <summary>
/// Encapsulates the output of a yt-dlp download operation.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class RunResult<T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool Success { get; }

    public string?[] ErrorOutput { get; }

    public T? Data { get; }

    /// <summary>
    /// Creates a new instance of class RunResult.
    /// </summary>
    public RunResult(bool success, string?[] error, T result)
    {
        Success = success;
        ErrorOutput = error;
        Data = result;
    }
}
