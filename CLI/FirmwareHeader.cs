using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace BoschFirmwareTool
{
    class FirmwareHeader
    {
        // This structure only consumes 0x40 bytes, but the file allocates 0x400. The rest is used for unknown purposes.
        public static readonly int HeaderLength = 0x400;

        public uint Magic { get; set; }
        public uint Target { get; set; }
        public uint Variant { get; set; }
        public uint Version { get; set; }
        public uint Length { get; set; }
        public uint Base { get; set; }
        public uint Checksum { get; set; } // Only present on the "root" header and subheaders. If headers are doubly nested, file header will not have a checksum.
        public uint Type { get; set; }
        public byte[] NegativeList { get; set; }
        public uint Offset { get; set; } // Offset into the file which it was found.

        public static FirmwareHeader Parse(ReadOnlySpan<byte> span)
        {
            if (span.Length < HeaderLength)
            {
                throw new ArgumentException($"input buffer too short", nameof(span));
            }

            return new FirmwareHeader
            {
                Magic = BinaryPrimitives.ReadUInt32BigEndian(span[0..4]),
                Target = BinaryPrimitives.ReadUInt32BigEndian(span[4..8]),
                Variant = BinaryPrimitives.ReadUInt32BigEndian(span[8..12]),
                Version = BinaryPrimitives.ReadUInt32BigEndian(span[12..16]),
                Length = BinaryPrimitives.ReadUInt32BigEndian(span[16..20]),
                Base = BinaryPrimitives.ReadUInt32BigEndian(span[20..24]),
                Checksum = BinaryPrimitives.ReadUInt32BigEndian(span[24..28]),
                Type = BinaryPrimitives.ReadUInt32BigEndian(span[28..32]),
                NegativeList = span[32..64].ToArray()
            };
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Firmware Header\n");
            sb.Append($"Target: {Target:X} ");
            sb.Append($"Variant: {Variant:X} ");
            sb.Append($"Version: {Version:X} ");
            sb.Append($"Length: {Length:X}");

            return sb.ToString();
        }
    }
}
