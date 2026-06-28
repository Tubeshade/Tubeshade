namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _quiet = new("-q", "--quiet");
    private Option<bool> _noQuiet = new("--no-quiet");
    private Option<bool> _noWarnings = new("--no-warnings");
    private Option<bool> _simulate = new("-s", "--simulate");
    private Option<bool> _noSimulate = new("--no-simulate");
    private Option<bool> _ignoreNoFormatsError = new("--ignore-no-formats-error");
    private Option<bool> _noIgnoreNoFormatsError = new("--no-ignore-no-formats-error");
    private Option<bool> _skipDownload = new("--skip-download", "--no-download");
    private MultiOption<string> _print = new("-O", "--print");
    private MultiOption<string> _printToFile = new("--print-to-file");
    private Option<bool> _dumpJson = new("-j", "--dump-json");
    private Option<bool> _dumpSingleJson = new("-J", "--dump-single-json");
    private Option<bool> _forceWriteArchive = new("--force-write-archive", "--force-download-archive");
    private Option<bool> _newline = new("--newline");
    private Option<bool> _noProgress = new("--no-progress");
    private Option<bool> _progress = new("--progress");
    private Option<bool> _consoleTitle = new("--console-title");
    private Option<string> _progressTemplate = new("--progress-template");
    private Option<string> _progressDelta = new("--progress-delta");
    private Option<bool> _verbose = new("-v", "--verbose");
    private Option<bool> _dumpPages = new("--dump-pages");
    private Option<bool> _writePages = new("--write-pages");
    private Option<bool> _printTraffic = new("--print-traffic");

    /// <summary>
    /// Activate quiet mode. If used with --verbose,
    /// print the log to stderr
    /// </summary>
    public bool Quiet
    {
        get => _quiet.Value;
        set => _quiet.Value = value;
    }

    /// <summary>
    /// Deactivate quiet mode. (Default)
    /// </summary>
    public bool NoQuiet
    {
        get => _noQuiet.Value;
        set => _noQuiet.Value = value;
    }

    /// <summary>
    /// Ignore warnings
    /// </summary>
    public bool NoWarnings
    {
        get => _noWarnings.Value;
        set => _noWarnings.Value = value;
    }

    /// <summary>
    /// Do not download the video and do not write
    /// anything to disk
    /// </summary>
    public bool Simulate
    {
        get => _simulate.Value;
        set => _simulate.Value = value;
    }

    /// <summary>
    /// Download the video even if printing/listing
    /// options are used
    /// </summary>
    public bool NoSimulate
    {
        get => _noSimulate.Value;
        set => _noSimulate.Value = value;
    }

    /// <summary>
    /// Ignore &quot;No video formats&quot; error. Useful for
    /// extracting metadata even if the videos are
    /// not actually available for download
    /// (experimental)
    /// </summary>
    public bool IgnoreNoFormatsError
    {
        get => _ignoreNoFormatsError.Value;
        set => _ignoreNoFormatsError.Value = value;
    }

    /// <summary>
    /// Throw error when no downloadable video
    /// formats are found (default)
    /// </summary>
    public bool NoIgnoreNoFormatsError
    {
        get => _noIgnoreNoFormatsError.Value;
        set => _noIgnoreNoFormatsError.Value = value;
    }

    /// <summary>
    /// Do not download the video but write all
    /// related files (Alias: --no-download)
    /// </summary>
    public bool SkipDownload
    {
        get => _skipDownload.Value;
        set => _skipDownload.Value = value;
    }

    /// <summary>
    /// Field name or output template to print to
    /// screen, optionally prefixed with when to
    /// print it, separated by a &quot;:&quot;. Supported
    /// values of &quot;WHEN&quot; are the same as that of
    /// --use-postprocessor (default: video).
    /// Implies --quiet. Implies --simulate unless
    /// --no-simulate or later stages of WHEN are
    /// used. This option can be used multiple times
    /// </summary>
    public MultiValue<string>? Print
    {
        get => _print.Value;
        set => _print.Value = value;
    }

    /// <summary>
    /// FILE
    /// Append given template to the file. The
    /// values of WHEN and TEMPLATE are the same as
    /// that of --print. FILE uses the same syntax
    /// as the output template. This option can be
    /// used multiple times
    /// </summary>
    public MultiValue<string>? PrintToFile
    {
        get => _printToFile.Value;
        set => _printToFile.Value = value;
    }

    /// <summary>
    /// Quiet, but print JSON information for each
    /// video. Simulate unless --no-simulate is
    /// used. See &quot;OUTPUT TEMPLATE&quot; for a
    /// description of available keys
    /// </summary>
    public bool DumpJson
    {
        get => _dumpJson.Value;
        set => _dumpJson.Value = value;
    }

    /// <summary>
    /// Quiet, but print JSON information for each
    /// URL or infojson passed. Simulate unless
    /// --no-simulate is used. If the URL refers to
    /// a playlist, the whole playlist information
    /// is dumped in a single line
    /// </summary>
    public bool DumpSingleJson
    {
        get => _dumpSingleJson.Value;
        set => _dumpSingleJson.Value = value;
    }

    /// <summary>
    /// Force download archive entries to be written
    /// as far as no errors occur, even if -s or
    /// another simulation option is used (Alias:
    /// --force-download-archive)
    /// </summary>
    public bool ForceWriteArchive
    {
        get => _forceWriteArchive.Value;
        set => _forceWriteArchive.Value = value;
    }

    /// <summary>
    /// Output progress bar as new lines
    /// </summary>
    public bool Newline
    {
        get => _newline.Value;
        set => _newline.Value = value;
    }

    /// <summary>
    /// Do not print progress bar
    /// </summary>
    public bool NoProgress
    {
        get => _noProgress.Value;
        set => _noProgress.Value = value;
    }

    /// <summary>
    /// Show progress bar, even if in quiet mode
    /// </summary>
    public bool Progress
    {
        get => _progress.Value;
        set => _progress.Value = value;
    }

    /// <summary>
    /// Display progress in console titlebar
    /// </summary>
    public bool ConsoleTitle
    {
        get => _consoleTitle.Value;
        set => _consoleTitle.Value = value;
    }

    /// <summary>
    /// Template for progress outputs, optionally
    /// prefixed with one of &quot;download:&quot; (default),
    /// &quot;download-title:&quot; (the console title),
    /// &quot;postprocess:&quot;,  or &quot;postprocess-title:&quot;.
    /// The video&#x27;s fields are accessible under the
    /// &quot;info&quot; key and the progress attributes are
    /// accessible under &quot;progress&quot; key. E.g.
    /// --console-title --progress-template
    /// &quot;download-
    /// title:%(info.id)s-%(progress.eta)s&quot;
    /// </summary>
    public string? ProgressTemplate
    {
        get => _progressTemplate.Value;
        set => _progressTemplate.Value = value;
    }

    /// <summary>
    /// Time between progress output (default: 0)
    /// </summary>
    public string? ProgressDelta
    {
        get => _progressDelta.Value;
        set => _progressDelta.Value = value;
    }

    /// <summary>
    /// Print various debugging information
    /// </summary>
    public bool Verbose
    {
        get => _verbose.Value;
        set => _verbose.Value = value;
    }

    /// <summary>
    /// Print downloaded pages encoded using base64
    /// to debug problems (very verbose)
    /// </summary>
    public bool DumpPages
    {
        get => _dumpPages.Value;
        set => _dumpPages.Value = value;
    }

    /// <summary>
    /// Write downloaded intermediary pages to files
    /// in the current directory to debug problems
    /// </summary>
    public bool WritePages
    {
        get => _writePages.Value;
        set => _writePages.Value = value;
    }

    /// <summary>
    /// Display sent and read HTTP traffic
    /// </summary>
    public bool PrintTraffic
    {
        get => _printTraffic.Value;
        set => _printTraffic.Value = value;
    }
}
