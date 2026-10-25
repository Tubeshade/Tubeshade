using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tubeshade.Server.Services.Ffmpeg;

public static class Mp4Layout
{
    private const uint Moov = 0x6D6F6F76;
    private const uint Moof = 0x6D6F6F66;
    private const uint Mdat = 0x6D646174;

    /// <summary>
    /// Checks whether the index (moov box) of an MP4 file is placed before the media data,
    /// and that the file is not fragmented, so that browsers can get the duration from the start of the file.
    /// </summary>
    public static async ValueTask<bool> IsStreamable(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, true);
        var length = stream.Length;
        var header = new byte[16];
        var hasIndex = false;

        while (stream.Position < length)
        {
            var offset = stream.Position;
            await stream.ReadExactlyAsync(header.AsMemory(0, 8), cancellationToken);
            var size = (long)BinaryPrimitives.ReadUInt32BigEndian(header);
            var type = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4));
            var headerSize = 8;

            if (size is 1)
            {
                await stream.ReadExactlyAsync(header.AsMemory(8, 8), cancellationToken);
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(8));
                headerSize = 16;
            }
            else if (size is 0)
            {
                size = length - offset;
            }

            if (size < headerSize)
            {
                throw new InvalidDataException($"Invalid box size {size} at offset {offset} in {path}");
            }

            switch (type)
            {
                case Moov:
                    hasIndex = true;
                    break;

                case Moof:
                    return false;

                case Mdat when !hasIndex:
                    return false;
            }

            stream.Position = offset + size;
        }

        return true;
    }
}
