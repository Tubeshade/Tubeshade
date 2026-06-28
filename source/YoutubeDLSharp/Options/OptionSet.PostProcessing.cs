namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _extractAudio = new("-x", "--extract-audio");
    private Option<AudioConversionFormat> _audioFormat = new("--audio-format");
    private Option<byte?> _audioQuality = new("--audio-quality");
    private Option<string> _remuxVideo = new("--remux-video");
    private Option<VideoRecodeFormat> _recodeVideo = new("--recode-video");
    private MultiOption<string> _postprocessorArgs = new("--postprocessor-args", "--ppa");
    private Option<bool> _keepVideo = new("-k", "--keep-video");
    private Option<bool> _noKeepVideo = new("--no-keep-video");
    private Option<bool> _postOverwrites = new("--post-overwrites");
    private Option<bool> _noPostOverwrites = new("--no-post-overwrites");
    private Option<bool> _embedSubs = new("--embed-subs");
    private Option<bool> _noEmbedSubs = new("--no-embed-subs");
    private Option<bool> _embedThumbnail = new("--embed-thumbnail");
    private Option<bool> _noEmbedThumbnail = new("--no-embed-thumbnail");
    private Option<bool> _embedMetadata = new("--embed-metadata", "--add-metadata");
    private Option<bool> _noEmbedMetadata = new("--no-embed-metadata", "--no-add-metadata");
    private Option<bool> _embedChapters = new("--embed-chapters", "--add-chapters");
    private Option<bool> _noEmbedChapters = new("--no-embed-chapters", "--no-add-chapters");
    private Option<bool> _embedInfoJson = new("--embed-info-json");
    private Option<bool> _noEmbedInfoJson = new("--no-embed-info-json");
    private Option<string> _parseMetadata = new("--parse-metadata");
    private MultiOption<string> _replaceInMetadata = new("--replace-in-metadata");
    private Option<bool> _xattrs = new("--xattrs");
    private Option<string> _concatPlaylist = new("--concat-playlist");
    private Option<string> _fixup = new("--fixup");
    private Option<string> _ffmpegLocation = new("--ffmpeg-location");
    private MultiOption<string> _exec = new("--exec");
    private Option<bool> _noExec = new("--no-exec");
    private Option<string> _convertSubs = new("--convert-subs");
    private Option<string> _convertThumbnails = new("--convert-thumbnails");
    private Option<bool> _splitChapters = new("--split-chapters");
    private Option<bool> _noSplitChapters = new("--no-split-chapters");
    private MultiOption<string> _removeChapters = new("--remove-chapters");
    private Option<bool> _noRemoveChapters = new("--no-remove-chapters");
    private Option<bool> _forceKeyframesAtCuts = new("--force-keyframes-at-cuts");
    private Option<bool> _noForceKeyframesAtCuts = new("--no-force-keyframes-at-cuts");
    private MultiOption<string> _usePostprocessor = new("--use-postprocessor");

    /// <summary>
    /// Convert video files to audio-only files
    /// (requires ffmpeg and ffprobe)
    /// </summary>
    public bool ExtractAudio
    {
        get => _extractAudio.Value;
        set => _extractAudio.Value = value;
    }

    /// <summary>
    /// Format to convert the audio to when -x is
    /// used. (currently supported: best (default),
    /// aac, alac, flac, m4a, mp3, opus, vorbis,
    /// wav). You can specify multiple rules using
    /// similar syntax as --remux-video
    /// </summary>
    public AudioConversionFormat AudioFormat
    {
        get => _audioFormat.Value;
        set => _audioFormat.Value = value;
    }

    /// <summary>
    /// Specify ffmpeg audio quality to use when
    /// converting the audio with -x. Insert a value
    /// between 0 (best) and 10 (worst) for VBR or a
    /// specific bitrate like 128K (default 5)
    /// </summary>
    public byte? AudioQuality
    {
        get => _audioQuality.Value;
        set => _audioQuality.Value = value;
    }

    /// <summary>
    /// Remux the video into another container if
    /// necessary (currently supported: avi, flv,
    /// gif, mkv, mov, mp4, webm, aac, aiff, alac,
    /// flac, m4a, mka, mp3, ogg, opus, vorbis,
    /// wav). If the target container does not
    /// support the video/audio codec, remuxing will
    /// fail. You can specify multiple rules; e.g.
    /// &quot;aac&gt;m4a/mov&gt;mp4/mkv&quot; will remux aac to m4a,
    /// mov to mp4 and anything else to mkv
    /// </summary>
    public string? RemuxVideo
    {
        get => _remuxVideo.Value;
        set => _remuxVideo.Value = value;
    }

    /// <summary>
    /// Re-encode the video into another format if
    /// necessary. The syntax and supported formats
    /// are the same as --remux-video
    /// </summary>
    public VideoRecodeFormat RecodeVideo
    {
        get => _recodeVideo.Value;
        set => _recodeVideo.Value = value;
    }

    /// <summary>
    /// Give these arguments to the postprocessors.
    /// Specify the postprocessor/executable name
    /// and the arguments separated by a colon &quot;:&quot;
    /// to give the argument to the specified
    /// postprocessor/executable. Supported PP are:
    /// Merger, ModifyChapters, SplitChapters,
    /// ExtractAudio, VideoRemuxer, VideoConvertor,
    /// Metadata, EmbedSubtitle, EmbedThumbnail,
    /// SubtitlesConvertor, ThumbnailsConvertor,
    /// FixupStretched, FixupM4a, FixupM3u8,
    /// FixupTimestamp and FixupDuration. The
    /// supported executables are: AtomicParsley,
    /// FFmpeg and FFprobe. You can also specify
    /// &quot;PP+EXE:ARGS&quot; to give the arguments to the
    /// specified executable only when being used by
    /// the specified postprocessor. Additionally,
    /// for ffmpeg/ffprobe, &quot;_i&quot;/&quot;_o&quot; can be
    /// appended to the prefix optionally followed
    /// by a number to pass the argument before the
    /// specified input/output file, e.g. --ppa
    /// &quot;Merger+ffmpeg_i1:-v quiet&quot;. You can use
    /// this option multiple times to give different
    /// arguments to different postprocessors.
    /// (Alias: --ppa)
    /// </summary>
    public MultiValue<string>? PostprocessorArgs
    {
        get => _postprocessorArgs.Value;
        set => _postprocessorArgs.Value = value;
    }

    /// <summary>
    /// Keep the intermediate video file on disk
    /// after post-processing
    /// </summary>
    public bool KeepVideo
    {
        get => _keepVideo.Value;
        set => _keepVideo.Value = value;
    }

    /// <summary>
    /// Delete the intermediate video file after
    /// post-processing (default)
    /// </summary>
    public bool NoKeepVideo
    {
        get => _noKeepVideo.Value;
        set => _noKeepVideo.Value = value;
    }

    /// <summary>
    /// Overwrite post-processed files (default)
    /// </summary>
    public bool PostOverwrites
    {
        get => _postOverwrites.Value;
        set => _postOverwrites.Value = value;
    }

    /// <summary>
    /// Do not overwrite post-processed files
    /// </summary>
    public bool NoPostOverwrites
    {
        get => _noPostOverwrites.Value;
        set => _noPostOverwrites.Value = value;
    }

    /// <summary>
    /// Embed subtitles in the video (only for mp4,
    /// webm and mkv videos)
    /// </summary>
    public bool EmbedSubs
    {
        get => _embedSubs.Value;
        set => _embedSubs.Value = value;
    }

    /// <summary>
    /// Do not embed subtitles (default)
    /// </summary>
    public bool NoEmbedSubs
    {
        get => _noEmbedSubs.Value;
        set => _noEmbedSubs.Value = value;
    }

    /// <summary>
    /// Embed thumbnail in the video as cover art
    /// </summary>
    public bool EmbedThumbnail
    {
        get => _embedThumbnail.Value;
        set => _embedThumbnail.Value = value;
    }

    /// <summary>
    /// Do not embed thumbnail (default)
    /// </summary>
    public bool NoEmbedThumbnail
    {
        get => _noEmbedThumbnail.Value;
        set => _noEmbedThumbnail.Value = value;
    }

    /// <summary>
    /// Embed metadata to the video file. Also
    /// embeds chapters/infojson if present unless
    /// --no-embed-chapters/--no-embed-info-json are
    /// used (Alias: --add-metadata)
    /// </summary>
    public bool EmbedMetadata
    {
        get => _embedMetadata.Value;
        set => _embedMetadata.Value = value;
    }

    /// <summary>
    /// Do not add metadata to file (default)
    /// (Alias: --no-add-metadata)
    /// </summary>
    public bool NoEmbedMetadata
    {
        get => _noEmbedMetadata.Value;
        set => _noEmbedMetadata.Value = value;
    }

    /// <summary>
    /// Add chapter markers to the video file
    /// (Alias: --add-chapters)
    /// </summary>
    public bool EmbedChapters
    {
        get => _embedChapters.Value;
        set => _embedChapters.Value = value;
    }

    /// <summary>
    /// Do not add chapter markers (default) (Alias:
    /// --no-add-chapters)
    /// </summary>
    public bool NoEmbedChapters
    {
        get => _noEmbedChapters.Value;
        set => _noEmbedChapters.Value = value;
    }

    /// <summary>
    /// Embed the infojson as an attachment to
    /// mkv/mka video files
    /// </summary>
    public bool EmbedInfoJson
    {
        get => _embedInfoJson.Value;
        set => _embedInfoJson.Value = value;
    }

    /// <summary>
    /// Do not embed the infojson as an attachment
    /// to the video file
    /// </summary>
    public bool NoEmbedInfoJson
    {
        get => _noEmbedInfoJson.Value;
        set => _noEmbedInfoJson.Value = value;
    }

    /// <summary>
    /// Parse additional metadata like title/artist
    /// from other fields; see &quot;MODIFYING METADATA&quot;
    /// for details. Supported values of &quot;WHEN&quot; are
    /// the same as that of --use-postprocessor
    /// (default: pre_process)
    /// </summary>
    public string? ParseMetadata
    {
        get => _parseMetadata.Value;
        set => _parseMetadata.Value = value;
    }

    /// <summary>
    /// Replace text in a metadata field using the
    /// given regex. This option can be used
    /// multiple times. Supported values of &quot;WHEN&quot;
    /// are the same as that of --use-postprocessor
    /// (default: pre_process)
    /// </summary>
    public MultiValue<string>? ReplaceInMetadata
    {
        get => _replaceInMetadata.Value;
        set => _replaceInMetadata.Value = value;
    }

    /// <summary>
    /// Write metadata to the video file&#x27;s xattrs
    /// (using Dublin Core and XDG standards)
    /// </summary>
    public bool Xattrs
    {
        get => _xattrs.Value;
        set => _xattrs.Value = value;
    }

    /// <summary>
    /// Concatenate videos in a playlist. One of
    /// &quot;never&quot;, &quot;always&quot;, or &quot;multi_video&quot;
    /// (default; only when the videos form a single
    /// show). All the video files must have the
    /// same codecs and number of streams to be
    /// concatenable. The &quot;pl_video:&quot; prefix can be
    /// used with &quot;--paths&quot; and &quot;--output&quot; to set
    /// the output filename for the concatenated
    /// files. See &quot;OUTPUT TEMPLATE&quot; for details
    /// </summary>
    public string? ConcatPlaylist
    {
        get => _concatPlaylist.Value;
        set => _concatPlaylist.Value = value;
    }

    /// <summary>
    /// Automatically correct known faults of the
    /// file. One of never (do nothing), warn (only
    /// emit a warning), detect_or_warn (the
    /// default; fix the file if we can, warn
    /// otherwise), force (try fixing even if the
    /// file already exists)
    /// </summary>
    public string? Fixup
    {
        get => _fixup.Value;
        set => _fixup.Value = value;
    }

    /// <summary>
    /// Location of the ffmpeg binary; either the
    /// path to the binary or its containing
    /// directory
    /// </summary>
    public string? FfmpegLocation
    {
        get => _ffmpegLocation.Value;
        set => _ffmpegLocation.Value = value;
    }

    /// <summary>
    /// Execute a command, optionally prefixed with
    /// when to execute it, separated by a &quot;:&quot;.
    /// Supported values of &quot;WHEN&quot; are the same as
    /// that of --use-postprocessor (default:
    /// after_move). The same syntax as the output
    /// template can be used to pass any field as
    /// arguments to the command. If no fields are
    /// passed, %(filepath,_filename|)q is appended
    /// to the end of the command. This option can
    /// be used multiple times
    /// </summary>
    public MultiValue<string>? Exec
    {
        get => _exec.Value;
        set => _exec.Value = value;
    }

    /// <summary>
    /// Remove any previously defined --exec
    /// </summary>
    public bool NoExec
    {
        get => _noExec.Value;
        set => _noExec.Value = value;
    }

    /// <summary>
    /// Convert the subtitles to another format
    /// (currently supported: ass, lrc, srt, vtt).
    /// Use &quot;--convert-subs none&quot; to disable
    /// conversion (default) (Alias: --convert-
    /// subtitles)
    /// </summary>
    public string? ConvertSubs
    {
        get => _convertSubs.Value;
        set => _convertSubs.Value = value;
    }

    /// <summary>
    /// Convert the thumbnails to another format
    /// (currently supported: jpg, png, webp). You
    /// can specify multiple rules using similar
    /// syntax as &quot;--remux-video&quot;. Use &quot;--convert-
    /// thumbnails none&quot; to disable conversion
    /// (default)
    /// </summary>
    public string? ConvertThumbnails
    {
        get => _convertThumbnails.Value;
        set => _convertThumbnails.Value = value;
    }

    /// <summary>
    /// Split video into multiple files based on
    /// internal chapters. The &quot;chapter:&quot; prefix can
    /// be used with &quot;--paths&quot; and &quot;--output&quot; to set
    /// the output filename for the split files. See
    /// &quot;OUTPUT TEMPLATE&quot; for details
    /// </summary>
    public bool SplitChapters
    {
        get => _splitChapters.Value;
        set => _splitChapters.Value = value;
    }

    /// <summary>
    /// Do not split video based on chapters
    /// (default)
    /// </summary>
    public bool NoSplitChapters
    {
        get => _noSplitChapters.Value;
        set => _noSplitChapters.Value = value;
    }

    /// <summary>
    /// Remove chapters whose title matches the
    /// given regular expression. The syntax is the
    /// same as --download-sections. This option can
    /// be used multiple times
    /// </summary>
    public MultiValue<string>? RemoveChapters
    {
        get => _removeChapters.Value;
        set => _removeChapters.Value = value;
    }

    /// <summary>
    /// Do not remove any chapters from the file
    /// (default)
    /// </summary>
    public bool NoRemoveChapters
    {
        get => _noRemoveChapters.Value;
        set => _noRemoveChapters.Value = value;
    }

    /// <summary>
    /// Force keyframes at cuts when
    /// downloading/splitting/removing sections.
    /// This is slow due to needing a re-encode, but
    /// the resulting video may have fewer artifacts
    /// around the cuts
    /// </summary>
    public bool ForceKeyframesAtCuts
    {
        get => _forceKeyframesAtCuts.Value;
        set => _forceKeyframesAtCuts.Value = value;
    }

    /// <summary>
    /// Do not force keyframes around the chapters
    /// when cutting/splitting (default)
    /// </summary>
    public bool NoForceKeyframesAtCuts
    {
        get => _noForceKeyframesAtCuts.Value;
        set => _noForceKeyframesAtCuts.Value = value;
    }

    /// <summary>
    /// The (case-sensitive) name of plugin
    /// postprocessors to be enabled, and
    /// (optionally) arguments to be passed to it,
    /// separated by a colon &quot;:&quot;. ARGS are a
    /// semicolon &quot;;&quot; delimited list of NAME=VALUE.
    /// The &quot;when&quot; argument determines when the
    /// postprocessor is invoked. It can be one of
    /// &quot;pre_process&quot; (after video extraction),
    /// &quot;after_filter&quot; (after video passes filter),
    /// &quot;video&quot; (after --format; before
    /// --print/--output), &quot;before_dl&quot; (before each
    /// video download), &quot;post_process&quot; (after each
    /// video download; default), &quot;after_move&quot;
    /// (after moving the video file to its final
    /// location), &quot;after_video&quot; (after downloading
    /// and processing all formats of a video), or
    /// &quot;playlist&quot; (at end of playlist). This option
    /// can be used multiple times to add different
    /// postprocessors
    /// </summary>
    public MultiValue<string>? UsePostprocessor
    {
        get => _usePostprocessor.Value;
        set => _usePostprocessor.Value = value;
    }
}
