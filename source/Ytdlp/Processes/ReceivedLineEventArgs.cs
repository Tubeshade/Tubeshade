namespace Ytdlp.Processes;

public readonly struct ReceivedLineEventArgs
{
    public ReceivedLineEventArgs(string line)
    {
        Line = line;
    }

    public string Line { get; }
}