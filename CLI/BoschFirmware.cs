using BoschFirmwareTool.Crypto;
using BoschFirmwareTool.Streams;
using boschfwtool.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using RSA = BoschFirmwareTool.Crypto.RSA;

namespace BoschFirmwareTool
{
    class BoschFirmware : IDisposable
    {
        private string _origFilename;
        private Stream _stream;

        private readonly RSA _cipher = new RSA(RSAKey.Modulus, RSAKey.PublicExponent);

        private readonly List<FirmwareHeader> _headers = new List<FirmwareHeader>();
        private bool _disposed = false;

        /// <summary>
        /// Create a new instance of <see cref="BoschFirmware"/> from a given file.
        /// </summary>
        /// <param name="filename">The path to a file containing a firmware image</param>
        /// <returns>An initialized instance of <see cref="BoschFirmware"/></returns>
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

        public event EventHandler<ExtractProgressEventArgs> ExtractProgress;

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

            // Examination of e.g. arm_boot_a5.fw.gz shows files with type 0x1 are not archives, but a single file
            // TODO: check rest of available firmwares to confirm this assumption.
            if (_headers[0].Type == 1)
            {
                ExtractRawFile(outDir);
                return;
            }

            // Grab non-nested headers, which have data
            var headers = _headers.Where((header) =>
            {
                return header.Target != (uint)FirmwareTargets.Nested;
            });

            foreach (var header in headers)
            {
                // For firmwares with multiple targets, parse everything into separate target directories
                var targetDir = Directory.CreateDirectory(Path.Join(outDir.FullName, $"{header.Target:X}"));

                using var dataStream = GetDataStream(header);
                ExtractArchive(dataStream, targetDir);
            }
        }

        // Used for extraction of archival formats in both firmware files and RomFS files.
        private void ExtractArchive(Stream dataStream, DirectoryInfo outDir)
        {
            var fileHdr = FileHeader.Parse(dataStream);

            while (fileHdr.FileLength != 0) // Read until terminating record found, which has zeroed attributes
            {
                using var file = OpenWrite(Path.Join(outDir.FullName, fileHdr.Filename));

                var fileBuf = new byte[fileHdr.OffsetToNext - FileHeader.HeaderLength]; // Offset is from header beginning, file length is from end of header
                dataStream.Read(fileBuf);
                // OffsetToNext is generally larger than FileLength. Files are padded to the nearest 16 byte boundary.
                file.Write(fileBuf, 0, (int)fileHdr.FileLength);

                OnExtractProgress(fileHdr.Filename, fileHdr.FileLength);

                // TODO: make configurable (disable/enable) via CLI flag?
                if (fileHdr.Filename.StartsWith("RomFS"))
                {
                    var romFsStream = new MemoryStream(fileBuf);
                    ExtractArchive(romFsStream, new DirectoryInfo(Path.Join(outDir.FullName, "RomFS")));
                }

                fileHdr = FileHeader.Parse(dataStream);
            }
        }

        private void ExtractRawFile(DirectoryInfo outDir)
        {
            var filename = _origFilename + ".out"; // TODO: make configurable?
            var rootHeader = _headers[0];

            using var dataStream = GetDataStream(rootHeader);
            using var fileStream = OpenWrite(Path.Join(outDir.FullName, filename));
            dataStream.CopyTo(fileStream);

            OnExtractProgress(filename, rootHeader.Length);
        }

        private FileStream OpenWrite(string filepath)
        {
            // Some archive files will have a relative directory, especially in RomFS situations. Ensure we've created the subdirectories.
            var fileInfo = new FileInfo(filepath);
            fileInfo.Directory.Create();
            return fileInfo.OpenWrite();
        }

        private void OnExtractProgress(string filename, long filelength)
        {
            var args = new ExtractProgressEventArgs(filename, filelength);
            ExtractProgress?.Invoke(this, args);
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
