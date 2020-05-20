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

        public uint Magic { get; private set; }
        public uint Target { get; private set; }
        public uint Variant { get; private set; }
        public uint Version { get; private set; }
        public uint Length { get; private set; }
        public uint Base { get; private set; }
        public uint Checksum { get; private set; }
        public uint Type { get; private set; }
        public byte[] NegativeList { get; private set; }

        public static FirmwareHeader Parse(ReadOnlySpan<byte> span)
        {
            if (span.Length < HeaderLength)
                throw new ArgumentException($"{nameof(span)} must be at least {HeaderLength} bytes long");

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
    }
}
