using BoschFirmwareTool;
using BoschFirmwareTool.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoschFirmwareTool
{
    class FirmwareFile : IDisposable
    {
        private Stream _stream;
        private bool _disposed = false;

        public FirmwareFile(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("stream is closed or unreadable");

            _stream = stream;

            // Grab root header and sanity check
            var header = ReadInitialHeader();
            if (header.Magic != Constants.FirmwareMagic)
                throw new InvalidDataException("firmware image invalid");

            FileHeader = header;
        }

        public FirmwareHeader FileHeader { get; private set; }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _stream?.Dispose();
            }

            _disposed = true;
        }

        private FirmwareHeader ReadInitialHeader()
        {
            Span<byte> buf = stackalloc byte[FirmwareHeader.HeaderLength];
            _stream.Read(buf);
            return FirmwareHeader.Parse(buf);
        }
    }
}
