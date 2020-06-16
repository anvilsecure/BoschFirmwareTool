using BoschFirmwareTool.Crypto;
using BoschFirmwareTool.Streams;
using boschfwtool.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;

using RSA = BoschFirmwareTool.Crypto.RSA;

namespace BoschFirmwareTool
{
    class BoschFirmware : IDisposable
    {
        private string _origFilename;
        private Stream _stream;

        private readonly RSA _cipher = new RSA(RSAKey.Modulus, RSAKey.PublicExponent);

        private readonly List<FirmwareHeader> _headers = new List<FirmwareHeader>();
        private readonly List<FirmwareFile> _files = new List<FirmwareFile>();
        private readonly List<FirmwareFile> _romfsFiles = new List<FirmwareFile>();
        private bool _disposed = false;

        public static BoschFirmware FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            var filehandle = File.OpenRead(filename);
            var fw = new BoschFirmware(filehandle)
            {
                _origFilename = Path.GetFileName(filename)
            };

            return fw;
        }

        private BoschFirmware(Stream stream)
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
                throw new InvalidDataException("firmware image invalid, bad magic");

            _headers.Add(header);

            // Get remaining file headers
            if (header.Target == (uint)FirmwareTargets.Nested)
            {
                GetSubheaders();
            }
        }

        public event EventHandler<ExtractProgressEventArgs> OnExtractProgress;

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

        public void ExtractAll(string outputDirectory)
        {
            if (String.IsNullOrEmpty(outputDirectory))
                throw new ArgumentNullException(nameof(outputDirectory));

            var outDir = Directory.CreateDirectory(outputDirectory);

            // Grab non-nested headers, which have data
            var headers = _headers.Where((header) =>
            {
                return header.Target != (uint)FirmwareTargets.Nested;
            });

            foreach (var header in headers)
            {
                using var dataStream = GetDataStream(header);
                Span<byte> hdrBuf = stackalloc byte[FileHeader.HeaderLength];

                dataStream.Read(hdrBuf);
                var fileHdr = FileHeader.Parse(hdrBuf);
                if (fileHdr.Magic != Constants.FileMagic) // Raw file instead of structured file set. Probably.
                {
                    var filename = _origFilename + ".bin"; // no metadata, guess the name. Usually only seen on single file firmware archives.
                    using var file = File.OpenWrite(Path.Combine(outDir.FullName, filename));
                    file.Write(hdrBuf);
                    dataStream.CopyTo(file);

                    var progress = new ExtractProgressEventArgs(filename, header.Length);
                    OnExtractProgress?.Invoke(this, progress);

                    continue;
                }

                while (fileHdr.FileLength != 0) // Read until terminating record found, which has zeroed attributes
                {
                    var filename = Path.GetFileName(fileHdr.Filename); // Filenames may have a path, create path if necessary.
                    var path = Path.GetDirectoryName(fileHdr.Filename);

                    if (!String.IsNullOrEmpty(path))
                        Directory.CreateDirectory(Path.Combine(outDir.FullName, path));

                    using var file = File.OpenWrite(Path.Combine(outDir.FullName, filename));

                    var fileBuf = new byte[fileHdr.OffsetToNext - FileHeader.HeaderLength]; // Offset is from header beginning, file length is from end of header
                    dataStream.Read(fileBuf);
                    file.Write(fileBuf, 0, (int)fileHdr.FileLength);

                    var progress = new ExtractProgressEventArgs(filename, fileHdr.FileLength);
                    OnExtractProgress?.Invoke(this, progress);

                    dataStream.Read(hdrBuf);
                    fileHdr = FileHeader.Parse(hdrBuf);
                }
            }
        }

        private void GetSubheaders()
        {
            uint offset = (uint)FirmwareHeader.HeaderLength;
            _stream.Seek(offset, SeekOrigin.Begin);

            while (offset < _stream.Length)
            {
                var header = ReadHeader(offset);
                _headers.Add(header);

                offset += (uint)(FirmwareHeader.HeaderLength + header.Length);
            }
        }

        private FirmwareHeader ReadHeader(long offset)
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

        private Stream GetDataStream(FirmwareHeader header)
        {
            // offset into the file past the header
            var substream = new SubStream(_stream, header.Offset + FirmwareHeader.HeaderLength, header.Length);

            var hasEncryptionKey = !header.KeyBlob.All(b => b == 0);
            if (hasEncryptionKey)
            {
                // Pull AES key from header, init AES decryptor, return stream
                var rawAesKey = _cipher.PublicDecrypt(header.KeyBlob);
                var aesKey = AESKey.Parse(rawAesKey);

                var aesAlg = new AesCryptoServiceProvider
                {
                    Key = aesKey.Key,
                    IV = aesKey.IV,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.None // No reference to padding seen in binary, using default (PKCS7) always fails.
                };

                var decryptor = aesAlg.CreateDecryptor();
                var cryptoStream = new CryptoStream(substream, decryptor, CryptoStreamMode.Read, leaveOpen: true);
                return cryptoStream;
            }
            else
            {
                return new XorStream(substream, Constants.DataXOR);
            }
        }
    }
}
