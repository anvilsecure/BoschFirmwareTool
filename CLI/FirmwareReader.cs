using BoschFirmwareTool;
using BoschFirmwareTool.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoschFirmwareTool
{
    class FirmwareReader : IDisposable
    {
        private Stream _source;
        private bool _disposed = false;

        public FirmwareReader(Stream source)
        {
            if (!source.CanRead)
            {
                throw new ArgumentException("stream closed or unreadable", nameof(source));
            }

            _source = source;
        }

        public FirmwareReader(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new ArgumentException(nameof(filename));
            }

            _source = File.OpenRead(filename);
        }

        public FirmwareFile Parse()
        {
            // Read firmware file into memory, sanity check it, build object graph
            _source.Seek(0, SeekOrigin.Begin); // Just in case parse is called multiple times
            Memory<byte> buf = new byte[_source.Length];
            _source.Read(buf.Span);

            // Grab the file header and validte magic, checksum
            var fileHeader = FirmwareHeader.Parse(buf.Span);
            if (fileHeader.Magic != Constants.FirmwareMagic)
            {
                throw new InvalidFirmwareException($"invalid magic in file header, got: {fileHeader.Magic:X}");
            }

            var checksum = Checksum32.Checksum(buf.Span[FirmwareHeader.HeaderLength..]);
            if (fileHeader.Checksum != checksum)
            {
                throw new InvalidFirmwareException($"Checksum mismatch, got: {checksum:X}, expected: {fileHeader.Checksum}");
            }

            return new FirmwareFile
            {
                FileHeader = fileHeader
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _source?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
