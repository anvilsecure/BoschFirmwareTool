using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace boschfwtool.Streams
{
    /// <summary>
    /// Wraps a <see cref="Stream"/>, providing access to a subsection of that stream.
    /// </summary>
    internal class SubStream : Stream
    {
        private Stream _innerStream;
        private readonly long _length;
        private long _position = 0;

        public SubStream(Stream stream, long offset, long length)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("stream must be readable / seekable");

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            _innerStream = stream;
            _length = length;

            _innerStream.Seek(offset, SeekOrigin.Begin);
        }
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position { get => _position; set => throw new NotImplementedException(); }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _length - _position;
            if (remaining <= 0)
                return 0;

            if (remaining < count)
                count = (int)remaining;

            var read = _innerStream.Read(buffer, offset, count);
            _position += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
