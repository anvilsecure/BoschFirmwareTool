using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace boschfwtool.Streams
{
    /// <summary>
    /// Implements a stream wrapper that performs an XOR operation on data processed with the stream.
    /// </summary>
    internal class XorStream : Stream
    {
        private Stream _innerStream;
        private byte _key;
        private bool _leaveOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="XorStream"/> class.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="key"></param>
        /// <param name="leaveOpen"></param>
        public XorStream(Stream stream, byte key, bool leaveOpen = true)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _innerStream = stream;
            _key = key;
            _leaveOpen = leaveOpen;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _innerStream.Read(buffer, offset, count);
            var buf = buffer.AsSpan();
            var readBuf = buf[offset..(offset + read)];
            for(int i = 0; i < read; i++)
            {
                readBuf[i] ^= _key;
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // TODO: XOR output for writing files.
            // Buffer 1K at a time, XOR, write?
            //_innerStream.Write(buffer, offset, count);
            throw new NotImplementedException();
        }
    }
}
