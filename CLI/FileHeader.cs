using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoschFirmwareTool
{
    class FileHeader
    {
        public static readonly int HeaderLength = 0x40;
        public uint Magic { get; set; }
        public uint OffsetToNext { get; set; }
        public string Filename { get; set; } // 32 bytes null-terminated
        public uint FileLength { get; set; } // Files are padded out to the nearest 0x10 byte boundary.

        public static FileHeader Parse(ReadOnlySpan<byte> span)
        {
            if (span.Length < HeaderLength)
                throw new ArgumentException("span too short");

            return new FileHeader
            {
                Magic = BinaryPrimitives.ReadUInt32BigEndian(span[0..4]),
                OffsetToNext = BinaryPrimitives.ReadUInt32BigEndian(span[4..8]),
                Filename = Encoding.ASCII.GetString(span[8..40]).TrimEnd('\0'),
                FileLength = BinaryPrimitives.ReadUInt32BigEndian(span[40..44])
            };
        }
    }
}
