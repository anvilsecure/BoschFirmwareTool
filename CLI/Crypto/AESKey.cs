using System;
using System.Buffers.Binary;

namespace BoschFirmwareTool.Crypto
{
    internal class AESKey
    {
        public byte[] Key { get; set; }
        public byte[] IV { get; set; }

        public static AESKey Parse(ReadOnlySpan<byte> span)
        {
            if (span.Length < 36)
                throw new ArgumentException("minimum key blob length is 36 bytes");

            var length = BinaryPrimitives.ReadUInt32BigEndian(span);
            if (length != 16 && length != 32)
                throw new ArgumentException("AES key length field not 128 or 256 bits");

            if (span.Length != (4 + length + 16)) // key length | key | IV
                throw new ArgumentException("key blob length does not match length field");

            return new AESKey
            {
                Key = span[4..(4 + (int)length)].ToArray(),
                IV = span[^16..].ToArray()
            };
        }
    }
}
