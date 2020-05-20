using boschfwtool;
using boschfwtool.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoschFirmwareTool
{
    class FirmwareFile
    {
        private readonly Memory<byte> _fileInMemory;

        public FirmwareHeader FileHeader { get; private set; }

        private FirmwareFile(Memory<byte> file)
        {
            _fileInMemory = file;
        }

        public bool Checksum()
        {
            // Each section header has a checksum, but we'll just use the first one, as it should refer to the whole file.
            var checksum = Checksum32.Checksum(_fileInMemory.Span[FirmwareHeader.HeaderLength..]);
            return checksum == FileHeader.Checksum;
        }

        public static FirmwareFile FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(nameof(filename));


            // Grab the file contents, pack them up, parse the initial header and confirm magic
            using var backingFile = File.OpenRead(filename);
            Memory<byte> fileContents = new byte[backingFile.Length]; // The largest files are ~300-400MB. Could be optimized to work on a stream.
            backingFile.Read(fileContents.Span);

            var fw = new FirmwareFile(fileContents)
            {
                FileHeader = FirmwareHeader.Parse(fileContents.Span)
            };

            if (fw.FileHeader.Magic != Constants.FirmwareMagic)
                throw new InvalidFirmwareException($"Header magic invalid, was: {fw.FileHeader.Magic:X}");

            return fw;
        }
    }
}
