using System;

namespace BoschFirmwareTool
{
    internal class ExtractProgressEventArgs : EventArgs
    {
        public string FileName { get; }
        public long FileSize { get; }

        public ExtractProgressEventArgs(string filename, long filesize)
        {
            FileName = filename;
            FileSize = filesize;
        }
    }
}
