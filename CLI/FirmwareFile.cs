namespace BoschFirmwareTool
{
    internal class FirmwareFile
    {
        public FileHeader Header { get; set; }
        public byte[] Contents { get; set; }
    }
}