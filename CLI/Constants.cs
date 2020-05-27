using System;
using System.Collections.Generic;
using System.Text;

namespace BoschFirmwareTool
{
    static class Constants
    {
        public static readonly uint FirmwareMagic = 0x10122003;
    }

    enum FirmwareTargets : uint
    {
        Nested = 0x10
    }
}
