using System;

namespace BoschFirmwareTool
{
    internal class ExtractProgressEventArgs : EventArgs
    {
        public string FileName { get; }
        public int FileSize { get; }
    }
}
