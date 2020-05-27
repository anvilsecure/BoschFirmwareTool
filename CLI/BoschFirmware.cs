using BoschFirmwareTool;
using BoschFirmwareTool.Exceptions;
using boschfwtool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BoschFirmwareTool
{
    class BoschFirmware : IDisposable
    {
        private Stream _stream;
        private FirmwareHeader _rootHeader;
        private List<FirmwareHeader> _subHeaders = new List<FirmwareHeader>();
        private List<FirmwareFile> _files = new List<FirmwareFile>();
        private bool _disposed = false;

        public BoschFirmware(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("stream is closed or unreadable");

            // Should probably copy it into memory in this case.
            if (!stream.CanSeek)
                throw new ArgumentException("stream is closed or unreadable");

            _stream = stream;

            // Grab root header and sanity check (magic, checksum)
            var header = ReadHeader(0);
            if (header.Magic != Constants.FirmwareMagic)
                throw new InvalidDataException("firmware image invalid");

            _rootHeader = header;

            // TODO: Main file header may have an empty checksum. Subheader(s) will have them set, we can check those.
            var calculatedChecksum = Checksum32.Checksum(_stream);
            if (calculatedChecksum != _rootHeader.Checksum)
                throw new InvalidDataException($"checksum mismatch, header: {_rootHeader.Checksum}, calculated: {calculatedChecksum}");

            // Get remaining file headers
            if (_rootHeader.Target == (uint)FirmwareTargets.Nested)
            {
                GetSubheaders();
            }

            // Setup file structures
            GetFiles();

            // return?
        }

        public IEnumerable<FirmwareHeader> Headers
        {
            get
            {
                yield return _rootHeader;
                foreach (var s in _subHeaders)
                    yield return s;
            }
        }

        public IEnumerable<FirmwareFile> Files => _files;

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

        private void GetFiles()
        {
            // Grab only the headers with data
            var headers = Headers.Where((fw) =>
            {
                return fw.Target != (uint)FirmwareTargets.Nested;
            });

            foreach (var h in headers)
            {
                // Read out FirmwareFile structures into list, expose dictionary by file name too?
            }
        }

        private void GetSubheaders()
        {
            uint offset = (uint)FirmwareHeader.HeaderLength;
            _stream.Seek(offset, SeekOrigin.Begin);

            while (offset < _stream.Length)
            {
                var header = ReadHeader(offset);
                _subHeaders.Add(header);

                offset += (uint)(FirmwareHeader.HeaderLength + header.Length);
            }
        }

        private FirmwareHeader ReadHeader(uint offset)
        {
            Span<byte> buf = stackalloc byte[FirmwareHeader.HeaderLength];
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Read(buf);
            var header = FirmwareHeader.Parse(buf);

            if (header.Magic != Constants.FirmwareMagic)
                throw new InvalidDataException($"read invalid header at {offset:X}, bad magic");

            header.Offset = offset;

            return header;
        }
    }
}
