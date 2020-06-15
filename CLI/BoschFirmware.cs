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
        private Stream _dataStream; // Wraps _stream for deobfuscating or decrypting data segments. Does not own / close _stream.

        private FirmwareHeader _rootHeader;

        private readonly RSA _cipher = new RSA(RSAKey.Modulus, RSAKey.PublicExponent);

        private List<FirmwareHeader> _subHeaders = new List<FirmwareHeader>();
        private List<FirmwareFile> _files = new List<FirmwareFile>();
        private List<FirmwareFile> _romfsFiles = new List<FirmwareFile>();
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

            _rootHeader = header;

            // Get remaining file headers
            if (_rootHeader.Target == (uint)FirmwareTargets.Nested)
            {
                GetSubheaders();
            }
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

            if (_subHeaders.Count == 0)
            {
                ExtractRawFile(outDir);
            }
            else
            {
                ExtractAllFiles(outDir);
            }
        }

        private void ExtractRawFile(DirectoryInfo outputDir)
        {

        }

        private void ExtractAllFiles(DirectoryInfo outputDir)
        {

        }

        private void GetRawFile()
        {
            var file = new FirmwareFile
            {
                Header = new FileHeader
                {
                    Filename = _origFilename + ".bin"
                }
            };

            using var dataStream = GetDataStream(_rootHeader);
            file.Contents = new byte[_rootHeader.Length];
            dataStream.Read(file.Contents);

            _files.Add(file);
        }

        private void GetRomFSFiles()
        {
            var files = _files.Where((file) =>
            {
                return file.Header.Filename.StartsWith("RomFS.bin");
            });

            foreach (var f in files)
            {
                using var romfsStream = new MemoryStream(f.Contents);
                Span<byte> headerBuf = stackalloc byte[FileHeader.HeaderLength];
                long offset = 0;

                while (offset < f.Header.FileLength)
                {
                    romfsStream.Seek(offset, SeekOrigin.Begin);
                    romfsStream.Read(headerBuf);
                    var header = FileHeader.Parse(headerBuf);
                    if (header.Magic != Constants.FileMagic)
                        throw new InvalidDataException($"invalid magic in RomFS file {f.Header.Filename}, offset: {offset:X}");

                    if (header.FileLength == 0)
                        break;

                    var contents = new byte[header.FileLength];
                    romfsStream.Read(contents);
                    var newFile = new FirmwareFile
                    {
                        Header = header,
                        Contents = contents
                    };

                    _romfsFiles.Add(newFile);
                    offset += header.OffsetToNext;
                }
            }
        }

        private void GetFiles()
        {
            // Grab only the headers with data
            var headers = Headers.Where((fw) =>
            {
                return fw.Target != (uint)FirmwareTargets.Nested;
            });

            Span<byte> _headerBuf = stackalloc byte[FileHeader.HeaderLength];
            foreach (var h in headers)
            {
                // Read out FirmwareFile structures into list, expose dictionary by file name too?
                var offset = h.Offset + FirmwareHeader.HeaderLength;

                while (offset < h.Length + h.Offset)
                {
                    _dataStream.Seek(offset, SeekOrigin.Begin);
                    _dataStream.Read(_headerBuf);
                    var fileHeader = FileHeader.Parse(_headerBuf);
                    if (fileHeader.Magic != Constants.FileMagic)
                        throw new InvalidDataException($"invalid magic at {offset:X}");

                    // File sections are terminated by a null record of sorts.
                    if (fileHeader.FileLength == 0)
                        break;

                    var contents = new byte[fileHeader.FileLength]; // TODO: sanity check this
                    _dataStream.Read(contents);

                    var file = new FirmwareFile
                    {
                        Header = fileHeader,
                        Contents = contents
                    };

                    _files.Add(file);

                    offset += fileHeader.OffsetToNext;
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
                _subHeaders.Add(header);

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
            var substream = new SubStream(_stream, header.Offset, header.Length);

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
                    Mode = CipherMode.CBC
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
