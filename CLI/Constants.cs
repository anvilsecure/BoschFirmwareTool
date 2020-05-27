using System;
using System.Collections.Generic;
using System.Text;

namespace BoschFirmwareTool
{
    static class Constants
    {
        public static readonly uint FirmwareMagic = 0x10122003;
        public static readonly uint FileMagic = 0xDEADAFFE;
        public static readonly byte DataXOR = 0x42;
    }

    enum FirmwareTargets : uint
    {
        Nested = 0x10
    }
}
